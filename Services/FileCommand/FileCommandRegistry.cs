using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace OneDesk.Services.FileCommand;

/// <summary>
/// 文件命令注册表，管理所有可用的文件命令
/// </summary>
public class FileCommandRegistry : IFileCommandRegistry
{
    private readonly List<IFileCommand> _commands;
    private readonly HashSet<string> _commandNames;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化文件命令注册表
    /// </summary>
    public FileCommandRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _commands = [];
        _commandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 在这里手动注册所有命令
        RegisterDefaultCommands();
    }

    /// <summary>
    /// 自动扫描并注册所有命令
    /// </summary>
    private void RegisterDefaultCommands()
    {
        // 获取当前程序集
        var assembly = Assembly.GetExecutingAssembly();

        // 定义目标命名空间常量，避免重复字符串比较
        const string targetNamespace = "OneDesk.Services.FileCommand.Commands";
        var commandInterfaceType = typeof(IFileCommand);

        // 扫描并过滤类型，将最快的条件前置
        var commandTypes = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false, Namespace: targetNamespace } &&
                           commandInterfaceType.IsAssignableFrom(type))
            .Select(type => type.AsType());

        // 实例化所有命令
        var commands = commandTypes
            .Select(type => (IFileCommand)ActivatorUtilities.CreateInstance(_serviceProvider, type))
            .OrderBy(c => c.Order).ToList();

        // 按 Order 排序后注册
        foreach (var command in commands)
        {
            Register(command);
        }
    }

    /// <summary>
    /// 注册一个命令
    /// </summary>
    /// <param name="command">要注册的命令</param>
    public void Register(IFileCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("命令名称不能为空", nameof(command));
        }

        // 使用 HashSet 快速检查是否已存在同名命令
        if (_commandNames.Contains(command.Name))
        {
            throw new InvalidOperationException($"命令 '{command.Name}' 已经注册");
        }

        _commands.Add(command);
        _commandNames.Add(command.Name);
    }

    /// <summary>
    /// 取消注册一个命令
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>如果成功取消注册返回 true，否则返回 false</returns>
    public bool Unregister(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return false;
        }

        var command = _commands.FirstOrDefault(c => string.Equals(c.Name, commandName, StringComparison.OrdinalIgnoreCase));

        if (command == null) return false;

        _commands.Remove(command);
        _commandNames.Remove(command.Name);
        return true;

    }

    /// <summary>
    /// 获取指定名称的命令
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>如果找到返回命令实例，否则返回 null</returns>
    public IFileCommand? GetCommand(string commandName)
    {
        return string.IsNullOrWhiteSpace(commandName)
            ? null
            : _commands.FirstOrDefault(c => string.Equals(c.Name, commandName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取所有已注册的命令
    /// </summary>
    /// <returns>所有命令的只读集合</returns>
    public IReadOnlyCollection<IFileCommand> GetAllCommands()
    {
        return _commands.AsReadOnly();
    }

    /// <summary>
    /// 获取可以在指定上下文中执行的所有命令（用于右键菜单）
    /// </summary>
    /// <param name="context">命令执行上下文</param>
    /// <returns>可执行命令的只读列表</returns>
    public IReadOnlyList<IFileCommand> GetExecutableCommands(FileCommandContext context)
    {
        return [.. _commands.Where(cmd => cmd.CanExecute(context))];
    }

    /// <summary>
    /// 检查命令是否已注册
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>如果已注册返回 true，否则返回 false</returns>
    public bool IsRegistered(string commandName)
    {
        return !string.IsNullOrWhiteSpace(commandName) && _commandNames.Contains(commandName);
    }

    /// <summary>
    /// 清空所有已注册的命令
    /// </summary>
    public void Clear()
    {
        _commands.Clear();
        _commandNames.Clear();
    }
}
