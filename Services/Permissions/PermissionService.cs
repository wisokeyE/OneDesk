using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.Invite;
using Microsoft.Graph.Models;
using OneDesk.Models.Permissions;
using OneDesk.Services.Auth;

namespace OneDesk.Services.Permissions;

/// <summary>
/// 权限服务实现，通过 Graph SDK 操作 OneDrive 文件权限
/// </summary>
public class PermissionService(IUserInfoManager userInfoManager) : IPermissionService
{
    private GraphServiceClient Client => userInfoManager.ActivatedClient
        ?? throw new InvalidOperationException("没有已激活的用户，无法执行权限操作。");

    public async Task<IList<Permission>> GetPermissionsAsync(string driveId, string itemId)
    {
        var response = await Client.Drives[driveId].Items[itemId].Permissions.GetAsync();
        return response?.Value ?? [];
    }

    public async Task<IList<Permission>> InviteUserAsync(string driveId, string itemId, string email, IList<ShareRole> roles)
    {
        var requestBody = new InvitePostRequestBody
        {
            Recipients = [new DriveRecipient { Email = email }],
            RequireSignIn = true,
            SendInvitation = false,
            Roles = [.. roles.Select(r => r.ToString())],
        };

        var response = await Client.Drives[driveId].Items[itemId].Invite.PostAsInvitePostResponseAsync(requestBody);
        return response?.Value ?? [];
    }

    public async Task DeletePermissionAsync(string driveId, string itemId, string permissionId)
    {
        await Client.Drives[driveId].Items[itemId].Permissions[permissionId].DeleteAsync();
    }
}
