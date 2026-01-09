using Microsoft.Graph.Models;
using OneDesk.Models;

namespace OneDesk.Services.FileCommand;

/// <summary>
/// 文件命令执行上下文，包含命令执行所需的所有信息
/// </summary>
public class FileCommandContext
{
    /// <summary>
    /// 当前选中的文件项列表
    /// </summary>
    public IReadOnlyList<DriveItem> SelectedItems { get; init; }

    /// <summary>
    /// 当前文件夹
    /// </summary>
    public DriveItem? CurrentFolder { get; init; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo UserInfo { get; init; }

    /// <summary>
    /// 附加参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>
    /// 初始化文件命令执行上下文
    /// </summary>
    /// <param name="selectedItems">当前选中的文件项列表</param>
    /// <param name="currentFolder">当前文件夹</param>
    /// <param name="userInfo">用户信息</param>
    public FileCommandContext(IReadOnlyList<DriveItem> selectedItems, DriveItem? currentFolder, UserInfo userInfo)
    {
        SelectedItems = selectedItems;
        CurrentFolder = currentFolder;
        UserInfo = userInfo;
    }

    /// <summary>
    /// 初始化文件命令执行上下文
    /// </summary>
    /// <param name="selectedItems">当前选中的文件项列表</param>
    /// <param name="currentFolder">当前文件夹</param>
    /// <param name="userInfo">用户信息</param>
    /// <param name="parameters">附加参数</param>
    public FileCommandContext(IReadOnlyList<DriveItem> selectedItems, DriveItem? currentFolder, UserInfo userInfo, Dictionary<string, object> parameters) : this(selectedItems, currentFolder, userInfo)
    {
        Parameters = parameters;
    }
}
