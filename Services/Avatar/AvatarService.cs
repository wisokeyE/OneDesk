using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Graph;
using OneDesk.Helpers.Dicebear;
using OneDesk.Services.Auth;
using SharpVectors.Renderers.Wpf;

namespace OneDesk.Services.Avatar;

public class AvatarService(IUserInfoManager manager) : IAvatarService
{
    public async Task<(bool isSvg, BitmapImage image)> GetUserImageAsync(
        GraphServiceClient? client, string displayName, string? userId = null)
    {
        client ??= manager.ActivatedClient!;

        Stream? content = null;
        // 分三种情况，null、empty、notEmpty
        if (userId is null or { Length: > 0 })
        {
            try
            {
                content = userId is null
                    ? await client.Me.Photo.Content.GetAsync()
                    : await client.Users[userId].Photo.Content.GetAsync();
            }
            catch (Exception)
            {
                //ignored
            }
        }

        var isSvg = false;
        MemoryStream memoryStream;
        if (content is null)
        {
            isSvg = true;
            var svgContent = InitialsGenerator.GenerateSvg(displayName);
            // 使用本地生成的 SVG
            memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent));
        }
        else
        {
            memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
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
            return (isSvg, image);
        }
    }
}