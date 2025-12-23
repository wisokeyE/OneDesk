namespace OneDesk.Services.FileCommand;

/// <summary>
/// 文件命令注册表接口
/// </summary>
public interface IFileCommandRegistry
{
    /// <summary>
    /// 注册一个命令
    /// </summary>
    /// <param name="command">要注册的命令</param>
    void Register(IFileCommand command);

    /// <summary>
    /// 取消注册一个命令
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>如果成功取消注册返回 true，否则返回 false</returns>
    bool Unregister(string commandName);

    /// <summary>
    /// 获取指定名称的命令
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>如果找到返回命令实例，否则返回 null</returns>
    IFileCommand? GetCommand(string commandName);

    /// <summary>
    /// 获取所有已注册的命令
    /// </summary>
    /// <returns>所有命令的只读集合</returns>
    IReadOnlyCollection<IFileCommand> GetAllCommands();

    /// <summary>
    /// 获取可以在指定上下文中执行的所有命令（用于右键菜单）
    /// </summary>
    /// <param name="context">命令执行上下文</param>
    /// <returns>可执行命令的只读列表</returns>
    IReadOnlyList<IFileCommand> GetExecutableCommands(FileCommandContext context);

    /// <summary>
    /// 检查命令是否已注册
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>如果已注册返回 true，否则返回 false</returns>
    bool IsRegistered(string commandName);

    /// <summary>
    /// 清空所有已注册的命令
    /// </summary>
    void Clear();
}
