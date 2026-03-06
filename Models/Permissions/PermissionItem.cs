using System.Windows.Media.Imaging;
using Microsoft.Graph.Models;
using OneDesk.Helpers;
using OneDesk.Services.Avatar;
using Application = System.Windows.Application;

namespace OneDesk.Models.Permissions;

/// <summary>
/// 权限条目模型，用于列表展示
/// </summary>
public partial class PermissionItem : ObservableObject
{
    public string PermissionId { get; }
    public string UserId { get; }
    public string DisplayName { get; }
    public string Email { get; }
    public string RolesDisplay { get; }
    public bool IsOwner { get; }
    public bool IsInherited { get; }
    [ObservableProperty]
    private BitmapImage? _photoBitmap;

    public PermissionItem(Permission permission)
    {
        PermissionId = permission.Id ?? string.Empty;

        var grantedTo = permission.GrantedToV2?.User ?? permission.GrantedTo?.User;
        UserId = grantedTo?.Id ?? string.Empty;
        DisplayName = grantedTo?.DisplayName ?? "共享链接";
        Email = grantedTo?.AdditionalData["email"]?.ToString() ?? string.Empty;

        RolesDisplay = permission.Roles is { Count: > 0 }
            ? string.Join(", ", permission.Roles.Select(r => Enum.Parse<ShareRole>(r).ToDisplayName()))
            : "未知";

        IsOwner = permission.Roles?.Contains(nameof(ShareRole.owner)) == true;
        IsInherited = permission.InheritedFrom != null;

        _ = LoadUserPhotoAsync();
    }

    private async Task LoadUserPhotoAsync()
    {
        var (_, image) = await CommonUtils.GetRequiredService<IAvatarService>().GetUserImageAsync(null, DisplayName, UserId);
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            PhotoBitmap = image;
        });
    }
}
