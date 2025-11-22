using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OneDesk.Models;
using OneDesk.Services.Auth;

namespace OneDesk.ViewModels.Pages;

public partial class FileManagerViewModel(IUserInfoManager userInfoManager) : ObservableObject
{

    [ObservableProperty]
    private ObservableCollection<DriveItem> _items = [];

    [ObservableProperty]
    private ObservableCollection<Item> _breadcrumbItems = [new("/", "/", null)];

    public Item CurrentFolder => BreadcrumbItems[^1];


    public async Task GetCurrentPathChildren()
    {
        var userInfo = userInfoManager.ActivatedUserInfo;
        if (userInfo is null)
        {
            Items = [];
            return;
        }
        if (!userInfo.IsInitialized)
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
