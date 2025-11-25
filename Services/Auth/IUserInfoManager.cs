using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Graph;
using OneDesk.Models;

namespace OneDesk.Services.Auth;

public interface IUserInfoManager : INotifyPropertyChanged
{
    public ReadOnlyObservableCollection<UserInfo> UserInfos { get; }

    public UserInfo? ActivatedUserInfo { get; }

    public GraphServiceClient? ActivatedClient { get; }

    public bool IsLocked { get; set; }

    public GraphServiceClient ActiveClient(int index);

    public GraphServiceClient AddClient();

    public void RemoveClient();
}