using System.Collections.ObjectModel;
using Microsoft.Graph;
using OneDesk.Models;

namespace OneDesk.Services.Auth;

public interface IUserInfoManager
{
    public ReadOnlyObservableCollection<UserInfo> UserInfos { get; }

    public UserInfo? ActivatedUserInfo { get; }

    public GraphServiceClient? ActivatedClient { get; }

    public GraphServiceClient ActiveClient(int index);

    public GraphServiceClient AddClient();

    public void RemoveClient();
}