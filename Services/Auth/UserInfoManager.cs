using System.Collections.ObjectModel;
using System.IO;
using Azure.Identity;
using Microsoft.Graph;
using OneDesk.Models;

namespace OneDesk.Services.Auth;

public partial class UserInfoManager : ObservableObject
{
    private readonly List<string> _files;

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
        _files = Directory.GetFiles(credentialFolderPath, "user*.json")
            .OrderBy(f => f)
            .ToList();
        UserInfos = new ReadOnlyObservableCollection<UserInfo>(_userInfos);
        Initialize();
    }

    private void Initialize()
    {
        UserInfo? activatedUserinfo = null;
        foreach (var file in _files)
        {
            var userinfo = BuildUserInfo(file);
            _userInfos.Add(userinfo);
            if (userinfo.UserInfoFile == _config.ActivatedUserFile)
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
        for (var i = 1; i <= _files.Count + 1; i++)
        {
            var filePath = Path.Combine(_config.CredentialFolderPath, "user" + i + ".json");
            if (Path.Exists(filePath)) continue;
            _files.Add(filePath);
            file = filePath;
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
        _files.Remove(oldActivateUserInfo.UserInfoFile);
        if (_userInfos.Count > 0)
        {
            ReplaceUserInfo(_userInfos.First());
        }
        else
        {
            ActivatedUserInfo = null;
            ActivatedClient = null;
            _config.ActivatedUserFile = null;
        }
        // 清理对应的 file
        if (Path.Exists(oldActivateUserInfo.UserInfoFile))
        {
            File.Delete(oldActivateUserInfo.UserInfoFile);
        }
        oldActivateUserInfo.Dispose();
    }

    private void ReplaceUserInfo(UserInfo userInfo)
    {
        if (ActivatedUserInfo is not null)
        {
            ActivatedUserInfo.IsActivated = false;
        }
        ActivatedUserInfo = userInfo;
        ActivatedUserInfo.IsActivated = true;
        ActivatedClient = ActivatedUserInfo.Client;
        _config.ActivatedUserFile = Path.GetFileName(ActivatedUserInfo.UserInfoFile);
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
