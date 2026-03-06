using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Azure.Core;
using Microsoft.Graph;
using OneDesk.Helpers;
using OneDesk.Models.Tasks;
using OneDesk.Services.Avatar;

namespace OneDesk.Models;

public partial class UserInfo : ObservableObject, IDisposable
{
    private static long _nextId;

    public long UserId { get; }

    [ObservableProperty]
    private string _displayName = "未知用户";

    [ObservableProperty]
    private string _mail = "未知邮箱";

    [ObservableProperty]
    private bool _isSvg;

    [ObservableProperty]
    private BitmapImage? _photoBitmap;

    [ObservableProperty]
    private string _userInfoFilePath;

    [ObservableProperty]
    private string? _driveId;

    [ObservableProperty]
    private bool _isUserInitialized;

    [ObservableProperty]
    private Item? _rootItem;

    [ObservableProperty]
    private ObservableCollection<Item> _sharedWithMeItems = [];

    [ObservableProperty]
    private UserTaskQueue? _taskQueue;

    public GraphServiceClient Client { get; }

    public TokenCredential Credential { get; }

    public Task InitializationTask { get; }

    public UserInfo(GraphServiceClient client, TokenCredential credential, string filePath)
    {
        Client = client;
        Credential = credential;
        UserInfoFilePath = filePath;
        UserId = Interlocked.Increment(ref _nextId);
        InitializationTask = InitUserInfoAsync();
    }

    private async Task InitUserInfoAsync()
    {
        try
        {
            var me = await Client.Me.GetAsync();
            var displayName = DisplayName;
            var mail = Mail;
            if (me is not null)
            {
                displayName = me.DisplayName!;
                mail = me.Mail!;
            }

            var drive = await Client.Me.Drive.GetAsync();
            var driveId = "";
            if (drive is not null)
            {
                driveId = drive.Id;
            }

            Item? rootItem = null;
            if (!string.IsNullOrEmpty(driveId))
            {
                var root = await Client.Drives[driveId].Root.GetAsync();
                rootItem = new Item("/", "/", null, root);
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 在UI线程更新属性
                DisplayName = displayName;
                Mail = mail;
                DriveId = driveId;
                RootItem = rootItem;
                IsUserInitialized = true;
                _ = RefreshSharedWithMeItemsAsync();
            });

            // 获取用户头像，放在最后执行，避免头像加载失败影响其他信息的初始化
            try
            {
                var (isSvg, image) = await CommonUtils.GetRequiredService<IAvatarService>().GetUserImageAsync(Client, displayName);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsSvg = isSvg;
                    PhotoBitmap = image;
                });
            }
            catch (Exception)
            {
                // 头像加载失败不影响用户信息初始化
            }
        }
        catch (Exception e)
        {
            await CommonUtils.ShowMessageBoxAsync("用户信息初始化失败", e.Message);
        }
    }

    public async Task RefreshSharedWithMeItemsAsync()
    {
        try
        {
            var items = new List<Item>();

            // 添加根目录项作为第一项
            if (RootItem != null)
            {
                items.Add(RootItem);
            }

            // 获取分享给用户的项
            var sharedItems = await Client.Drives[DriveId].SharedWithMe.GetAsSharedWithMeGetResponseAsync();
            if (sharedItems?.Value != null)
            {
                items.AddRange(sharedItems.Value.Select(driveItem => new Item(driveItem.Name ?? "未命名", driveItem.Name ?? "", null, driveItem)));
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SharedWithMeItems = new ObservableCollection<Item>(items);
            });
        }
        catch (Exception e)
        {
            await CommonUtils.ShowMessageBoxAsync("刷新分享列表失败", e.Message);
        }
    }

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}