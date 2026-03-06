using Microsoft.Graph.Models;
using OneDesk.Models.Permissions;

namespace OneDesk.Services.Permissions;

/// <summary>
/// 权限服务接口，封装 OneDrive 文件权限操作
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// 获取文件或文件夹的权限列表
    /// </summary>
    Task<IList<Permission>> GetPermissionsAsync(string driveId, string itemId);

    /// <summary>
    /// 邀请用户获得文件权限
    /// </summary>
    Task<IList<Permission>> InviteUserAsync(string driveId, string itemId, string email, IList<ShareRole> roles);

    /// <summary>
    /// 删除文件权限
    /// </summary>
    Task DeletePermissionAsync(string driveId, string itemId, string permissionId);
}
