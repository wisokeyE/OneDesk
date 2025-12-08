using System.Collections.ObjectModel;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OneDesk.Models;
using OneDesk.Services.Auth;

namespace OneDesk.ViewModels.Pages;

public partial class FileManagerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DriveItem> _items = [];

    [ObservableProperty]
    private ObservableCollection<Item> _breadcrumbItems = [new("/", "/", null)];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _rootSwitchPopupIsOpen;

    [ObservableProperty]
    private IUserInfoManager _userInfoManager;

    [ObservableProperty]
    private int _rootIndex;

    public Item CurrentFolder => BreadcrumbItems[^1];

    partial void OnBreadcrumbItemsChanged(ObservableCollection<Item> value)
    {
        OnPropertyChanged(nameof(CurrentFolder));

        // 每次集合被替换时，重新绑定 CollectionChanged 监听
        value.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentFolder));
        };
    }

    public FileManagerViewModel(IUserInfoManager userInfoManager)
    {
        _userInfoManager = userInfoManager;

        // 初始集合的监听会在 OnBreadcrumbItemsChanged 中设置
        OnBreadcrumbItemsChanged(BreadcrumbItems);

        _userInfoManager.PropertyChanged += (v, e) =>
        {
            if (e.PropertyName != nameof(IUserInfoManager.ActivatedUserInfo)) return;
            BreadcrumbItems = [UserInfoManager.ActivatedUserInfo!.RootItem!];
            RootIndex = 0;
            _ = GetCurrentPathChildren();
        };
    }

    public async Task GetCurrentPathChildren()
    {
        UserInfoManager.IsLocked = true;
        IsLoading = true;
        try
        {
            var userInfo = UserInfoManager.ActivatedUserInfo;
            if (userInfo is null)
            {
                Items = [];
                return;
            }
            if (!userInfo.IsUserInitialized)
            {
                await userInfo.InitializationTask; // 等待用户信息初始化完成
            }

            if (BreadcrumbItems[0].DriveItem is null)
            {
                BreadcrumbItems = [userInfo.RootItem!];
            }
            var client = userInfo.Client;

            try
            {
                var response = await GetItemChildrenByPath(client, CurrentFolder);
                Items = response?.Value is { Count: > 0 } ? new ObservableCollection<DriveItem>(response.Value) : [];
            }
            catch
            {
                Items = [];
            }
        }
        finally
        {
            IsLoading = false;
            UserInfoManager.IsLocked = false;
        }
    }

    [RelayCommand]
    private void OnRefresh()
    {
        _ = GetCurrentPathChildren();
    }

    [RelayCommand]
    private void OnUp()
    {
        if (CurrentFolder.ParentFolder is null) return;
        BreadcrumbItems.RemoveAt(BreadcrumbItems.Count - 1);
        _ = GetCurrentPathChildren();
    }

    [RelayCommand]
    private void OnJump(object item)
    {
        if (item is not Item targetItem) return;

        var index = BreadcrumbItems.IndexOf(targetItem);
        // 移除目标项之后的所有项
        for (var i = BreadcrumbItems.Count - 1; i > index; i--)
        {
            BreadcrumbItems.RemoveAt(i);
        }
        _ = GetCurrentPathChildren();
    }

    [RelayCommand]
    private void OnToggleRootSwitch()
    {
        if (UserInfoManager.ActivatedUserInfo != null)
        {
            _ = UserInfoManager.ActivatedUserInfo.RefreshSharedWithMeItemsAsync();
        }
        RootSwitchPopupIsOpen = !RootSwitchPopupIsOpen;
    }

    [RelayCommand]
    private void OnSwitchRoot(Item item)
    {
        // 直接替换，以避免出现集合为空的中间态
        BreadcrumbItems = [item];
        RootSwitchPopupIsOpen = false;
        _ = GetCurrentPathChildren();
    }

    private static async Task<DriveItemCollectionResponse?> GetItemChildrenByPath(GraphServiceClient client, Item item)
    {
        var driveItem = item.DriveItem!;
        var parentReference = driveItem.ParentReference is null ? driveItem.RemoteItem!.ParentReference! : driveItem.ParentReference!;
        var driveId = parentReference.DriveId!;
        return await client.Drives[driveId].Items[driveItem.Id].Children.GetAsync();
    }
}
