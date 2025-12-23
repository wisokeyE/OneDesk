namespace OneDesk.Services.FileCommand;

/// <summary>
/// 文件命令接口，定义文件操作的基本契约
/// </summary>
public interface IFileCommand
{
    /// <summary>
    /// 命令名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 命令在菜单中的显示顺序，数值越小越靠前
    /// </summary>
    int Order { get; }

    /// <summary>
    /// 判断命令是否可以执行
    /// </summary>
    /// <param name="context">命令执行上下文</param>
    /// <returns>如果可以执行返回 true，否则返回 false</returns>
    bool CanExecute(FileCommandContext context);

    /// <summary>
    /// 异步执行命令
    /// </summary>
    /// <param name="context">命令执行上下文</param>
    /// <returns>表示异步操作的任务</returns>
    Task ExecuteAsync(FileCommandContext context);
}
