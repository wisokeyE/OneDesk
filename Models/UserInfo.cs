using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using Azure.Core;
using Microsoft.Graph;
using OneDesk.Helpers;
using OneDesk.Helpers.Dicebear;
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
    private string? _driveId;

    [ObservableProperty]
    private bool _isUserInitialized;

    public GraphServiceClient Client { get; private set; }

    public TokenCredential Credential { get; private set; }

    public Task InitializationTask { get; }

    public UserInfo(GraphServiceClient client, TokenCredential credential, string filePath)
    {
        Client = client;
        Credential = credential;
        UserInfoFilePath = filePath;
        InitializationTask = InitUserInfoAsync();
    }

    private async Task InitUserInfoAsync()
    {
        var displayName = DisplayName;
        var mail = Mail;
        var driveId = "";
        var isSvg = IsSvg;
        var photoUrl = PhotoUrl;
        var svgContent = "";
        try
        {
            var me = await Client.Me.GetAsync();
            if (me is not null)
            {
                displayName = me.DisplayName!;
                mail = me.Mail!;
            }

            var drive = await Client.Me.Drive.GetAsync();
            if (drive is not null)
            {
                driveId = drive.Id;
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
                svgContent = InitialsGenerator.GenerateSvg(displayName);
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 在UI线程更新属性
                DisplayName = displayName;
                Mail = mail;
                DriveId = driveId;
                IsSvg = isSvg;
                PhotoUrl = photoUrl;
                IsUserInitialized = true;
            });

            // 缓存图片到内存流中
            try
            {
                MemoryStream memoryStream;
                if (string.IsNullOrWhiteSpace(svgContent))
                {
                    // 从网络下载图片
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetByteArrayAsync(photoUrl);
                    memoryStream = new MemoryStream(response);
                }
                else
                {
                    // 使用本地生成的 SVG
                    memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent));
                }

                using (memoryStream)
                {
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
                        PhotoBitmap = image;
                    });
                }
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

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}