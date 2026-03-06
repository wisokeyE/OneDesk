namespace OneDesk.Models.Permissions;

/// <summary>
/// OneDrive 共享权限角色枚举（枚举名与 Graph API 角色字符串大小写一致）
/// </summary>
public enum ShareRole
{
    /// <summary>
    /// 可查看
    /// </summary>
    read,
    /// <summary>
    /// 可编辑
    /// </summary>
    write,
    /// <summary>
    /// 所有者
    /// </summary>
    owner,
}

public static class ShareRoleExtensions
{
    /// <summary>
    /// 将枚举转换为界面显示名称
    /// </summary>
    public static string ToDisplayName(this ShareRole role) => role switch
    {
        ShareRole.owner => "所有者",
        ShareRole.write => "可编辑",
        ShareRole.read => "可查看",
        _ => role.ToString(),
    };
}
