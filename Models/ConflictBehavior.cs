namespace OneDesk.Models;

/// <summary>
/// 名称冲突时的解决方案
/// </summary>
public enum ConflictBehavior
{
    /// <summary>
    /// 替换现有文件/文件夹
    /// </summary>
    replace,

    /// <summary>
    /// 重命名新文件/文件夹
    /// </summary>
    rename,

    /// <summary>
    /// 失败并报错
    /// </summary>
    fail
}
