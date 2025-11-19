using System.Collections.ObjectModel;
using System.IO;
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

    public ReadOnlyObservableCollection<UserInfo> UserInfos { get; }

    [ObservableProperty]
    private UserInfo? _activatedUserInfo;

    [ObservableProperty]
    private GraphServiceClient? _activatedClient;

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
            .OrderBy(f => f)
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
        ReplaceUserInfo(UserInfos[index]);
        return ActivatedClient!;
    }

    public GraphServiceClient AddClient()
    {
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
        if (ActivatedUserInfo is null) return;
        var oldActivateUserInfo = ActivatedUserInfo;
        _userInfos.Remove(oldActivateUserInfo);
        _filePaths.Remove(oldActivateUserInfo.UserInfoFilePath);
        if (_userInfos.Count > 0)
        {
            ReplaceUserInfo(_userInfos.First());
        }
        else
        {
            ActivatedUserInfo = null;
            ActivatedClient = null;
            _config.ActivatedUserFileName = null;
        }
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
        ActivatedClient = ActivatedUserInfo.Client;
        _config.ActivatedUserFileName = Path.GetFileName(ActivatedUserInfo.UserInfoFilePath);
    }

    private static UserInfo BuildUserInfo(string filePath)
    {
        var credential = new FileBackDeviceCodeCredential(new DeviceCodeCredentialOptions
        {
            ClientId = _config.ClientId,
            TenantId = string.IsNullOrWhiteSpace(_config.TenantId) ? null : _config.TenantId
        }, filePath);
        var graphServiceClient = new GraphServiceClient(credential, Scopes);

        var userinfo = new UserInfo(graphServiceClient, credential, filePath);
        return userinfo;
    }
}
