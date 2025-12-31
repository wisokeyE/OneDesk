using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using Azure.Identity;
using Microsoft.Graph;
using OneDesk.Models;

namespace OneDesk.Services.Auth;

public partial class UserInfoManager : ObservableObject, IUserInfoManager
{
    private readonly List<string> _filePaths;

    private static AppConfig _config = null!;

    private static readonly string[] Scopes = ["https://graph.microsoft.com/.default"];

    private readonly ObservableCollection<UserInfo> _userInfos = [];

    private int _nextUserId = 1;

    public ReadOnlyObservableCollection<UserInfo> UserInfos { get; }

    [ObservableProperty]
    private UserInfo? _activatedUserInfo;

    public GraphServiceClient? ActivatedClient => ActivatedUserInfo?.Client;

    public bool IsLocked { get; set; }

    [GeneratedRegex(@"^user\d+$")]
    private static partial Regex UserFileNameRegex();

    public UserInfoManager(AppConfig config)
    {
        _config = config;
        var credentialFolderPath = config.CredentialFolderPath;
        // 确保目录存在
        if (string.IsNullOrWhiteSpace(credentialFolderPath)) credentialFolderPath = ".";
        credentialFolderPath = Path.GetFullPath(credentialFolderPath);
        _config = config;
        if (!Directory.Exists(credentialFolderPath))
        {
            Directory.CreateDirectory(credentialFolderPath);
        }
        // 读取目录，获取所有的user*.json文件
        _filePaths = Directory.GetFiles(credentialFolderPath, "user*.json")
            .Select(f => new { FilePath = f, Name = Path.GetFileNameWithoutExtension(f) })
            .Where(f => UserFileNameRegex().IsMatch(f.Name))
            .OrderBy(f => int.Parse(f.Name[4..]))
            .Select(f => f.FilePath)
            .ToList();
        UserInfos = new ReadOnlyObservableCollection<UserInfo>(_userInfos);
        Initialize();
    }

    private void Initialize()
    {
        UserInfo? activatedUserinfo = null;
        foreach (var filePath in _filePaths)
        {
            var userinfo = BuildUserInfo(filePath);
            _userInfos.Add(userinfo);
            if (Path.GetFileName(userinfo.UserInfoFilePath) == _config.ActivatedUserFileName)
            {
                activatedUserinfo = userinfo;
            }
        }
        if (UserInfos.Count > 0 && activatedUserinfo is null)
        {
            activatedUserinfo = UserInfos[0];
        }

        if (activatedUserinfo is null) return;
        ReplaceUserInfo(activatedUserinfo);
    }

    public GraphServiceClient ActiveClient(int index)
    {
        if (IsLocked) return ActivatedClient!;
        ReplaceUserInfo(UserInfos[index]);
        return ActivatedClient!;
    }

    public GraphServiceClient AddClient()
    {
        if (IsLocked) return ActivatedClient!;
        // 计算下一个文件名
        var file = "";
        for (var i = 1; i <= _filePaths.Count + 1; i++)
        {
            var tempFilePath = Path.Combine(_config.CredentialFolderPath, "user" + i + ".json");
            if (Path.Exists(tempFilePath)) continue;
            _filePaths.Add(tempFilePath);
            file = tempFilePath;
            break;
        }
        if (string.IsNullOrWhiteSpace(file))
        {
            // 这里理论上不会发生，但是如果进来了，就抛出异常
            throw new InvalidOperationException("无法生成新的凭据文件名。");
        }

        var userinfo = BuildUserInfo(file);
        _userInfos.Add(userinfo);
        ReplaceUserInfo(userinfo);
        return ActivatedClient!;
    }

    public void RemoveClient()
    {
        if (IsLocked || ActivatedUserInfo is null) return;
        var oldActivateUserInfo = ActivatedUserInfo;
        // 先计算下一个要激活的 userinfo
        // 如果用户数大于1，则判断当前激活的是不是第一个，是则激活第二个，否则激活第一个
        // 如果用户数小于等于1，则删除后不激活任何用户
        if (_userInfos.Count > 1)
        {
            var newActivatedUserInfo = _userInfos[0] == oldActivateUserInfo ? _userInfos[1] : _userInfos[0];
            ReplaceUserInfo(newActivatedUserInfo);
        }
        else
        {
            ActivatedUserInfo = null;
            _config.ActivatedUserFileName = null;
        }
        // 然后处理集合，以避免出现ActivatedUserInfo不在集合中的情况
        _userInfos.Remove(oldActivateUserInfo);
        _filePaths.Remove(oldActivateUserInfo.UserInfoFilePath);
        // 清理对应的 file
        if (Path.Exists(oldActivateUserInfo.UserInfoFilePath))
        {
            File.Delete(oldActivateUserInfo.UserInfoFilePath);
        }
        oldActivateUserInfo.Dispose();
    }

    private void ReplaceUserInfo(UserInfo userInfo)
    {
        ActivatedUserInfo = userInfo;
        _config.ActivatedUserFileName = Path.GetFileName(ActivatedUserInfo.UserInfoFilePath);
    }

    private UserInfo BuildUserInfo(string filePath)
    {
        var credential = new FileBackDeviceCodeCredential(new DeviceCodeCredentialOptions
        {
            ClientId = _config.ClientId,
            TenantId = string.IsNullOrWhiteSpace(_config.TenantId) ? null : _config.TenantId
        }, filePath);
        var graphServiceClient = new GraphServiceClient(credential, Scopes);

        var userinfo = new UserInfo(graphServiceClient, credential, filePath, _nextUserId++);
        return userinfo;
    }
}
