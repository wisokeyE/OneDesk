using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using Azure.Core;
using Microsoft.Graph;
using SharpVectors.Renderers.Wpf;

namespace OneDesk.Models;

public partial class UserInfo : ObservableObject, IDisposable
{
    [ObservableProperty]
    private string _displayName = "未知用户";

    [ObservableProperty]
    private string _mail = "未知邮箱";

    [ObservableProperty]
    private string? _photoUrl;

    [ObservableProperty]
    private bool _isSvg;

    [ObservableProperty]
    private BitmapImage? _photoBitmap;

    [ObservableProperty]
    private string _userInfoFilePath;

    [ObservableProperty]
    private bool _isInitialized;

    public GraphServiceClient Client { get; private set; }

    public TokenCredential Credential { get; private set; }

    public UserInfo(GraphServiceClient client, TokenCredential credential, string filePath)
    {
        Client = client;
        Credential = credential;
        UserInfoFilePath = filePath;
        _ = InitUserInfoAsync();
    }

    private async Task InitUserInfoAsync()
    {
        var displayName = DisplayName;
        var mail = Mail;
        var isSvg = IsSvg;
        var photoUrl = PhotoUrl;
        var me = await Client.Me.GetAsync();
        if (me is not null)
        {
            displayName = me.DisplayName!;
            mail = me.Mail!;
        }

        try
        {
            var photo = await Client.Me.Photo.GetAsync();
            photoUrl = photo?.Id;
        }
        catch (Exception)
        {
            //ignored
        }

        if (string.IsNullOrWhiteSpace(photoUrl))
        {
            isSvg = true;
            photoUrl = "https://api.dicebear.com/9.x/initials/svg?seed=" + displayName + "&chars=1";
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // 在UI线程更新属性
            DisplayName = displayName;
            Mail = mail;
            IsSvg = isSvg;
            PhotoUrl = photoUrl;
        });

        // 缓存图片到内存流中
        using var httpClient = new HttpClient();
        var response = await httpClient.GetByteArrayAsync(photoUrl);
        using var memoryStream = new MemoryStream(response);
        var image = new BitmapImage();
        if (isSvg)
        {
            var settings = new WpfDrawingSettings
            {
                IncludeRuntime = true,
                TextAsGeometry = false
            };
            var converter = new SharpVectors.Converters.StreamSvgConverter(settings);
            using var imageStream = new MemoryStream();
            converter.Convert(memoryStream, imageStream);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = imageStream;
            image.EndInit();
        }
        else
        {
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memoryStream;
            image.EndInit();
        }
        image.Freeze();
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // 在UI线程更新属性
            PhotoBitmap = image;
            IsInitialized = true;
        });
    }

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}