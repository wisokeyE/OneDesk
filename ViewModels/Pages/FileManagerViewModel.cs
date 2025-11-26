using System.Collections.ObjectModel;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OneDesk.Models;
using OneDesk.Services.Auth;

namespace OneDesk.ViewModels.Pages;

public partial class FileManagerViewModel : ObservableObject
{
    private static readonly Item RootItem = new("/", "/", null);

    [ObservableProperty]
    private ObservableCollection<DriveItem> _items = [];

    [ObservableProperty]
    private ObservableCollection<Item> _breadcrumbItems = [RootItem];

    [ObservableProperty]
    private bool _isLoading;

    private readonly IUserInfoManager _userInfoManager;

    public Item CurrentFolder => BreadcrumbItems[^1];

    partial void OnBreadcrumbItemsChanged(ObservableCollection<Item> value)
    {
        OnPropertyChanged(nameof(CurrentFolder));
    }

    public FileManagerViewModel(IUserInfoManager userInfoManager)
    {
        _userInfoManager = userInfoManager;
        BreadcrumbItems.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentFolder));
        };
        _userInfoManager.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IUserInfoManager.ActivatedUserInfo))
            {
                BreadcrumbItems = [RootItem];
                _ = GetCurrentPathChildren();
            }
        };
    }

    public async Task GetCurrentPathChildren()
    {
        _userInfoManager.IsLocked = true;
        IsLoading = true;
        try
        {
            var userInfo = _userInfoManager.ActivatedUserInfo;
            if (userInfo is null)
            {
                Items = [];
                return;
            }
            if (!userInfo.IsUserInitialized)
            {
                await userInfo.InitializationTask; // Wait until user info (DriveId) initialized
            }
            var client = userInfo.Client;

            try
            {
                var response = await GetItemChildrenByPath(client, userInfo.DriveId!, CurrentFolder.Path);
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
            _userInfoManager.IsLocked = false;
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
        // Remove all items after the target item
        for (var i = BreadcrumbItems.Count - 1; i > index; i--)
        {
            BreadcrumbItems.RemoveAt(i);
        }
        _ = GetCurrentPathChildren();
    }

    private static async Task<DriveItemCollectionResponse?> GetItemChildrenByPath(GraphServiceClient client, string driveId, string path)
    {
        var rootRequestBuilder = client.Drives[driveId].Root;
        if (!string.IsNullOrWhiteSpace(path) && path != "/")
        {
            return await rootRequestBuilder.ItemWithPath(path).Children.GetAsync();
        }
        var root = await rootRequestBuilder.GetAsync();
        return await client.Drives[driveId].Items[root!.Id].Children.GetAsync();
    }
}
