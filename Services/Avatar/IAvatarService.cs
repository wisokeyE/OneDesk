using System.Windows.Media.Imaging;
using Microsoft.Graph;

namespace OneDesk.Services.Avatar;

public interface IAvatarService
{
    Task<(bool isSvg, BitmapImage image)> GetUserImageAsync(GraphServiceClient? client, string displayName,
        string? userId = null);
}