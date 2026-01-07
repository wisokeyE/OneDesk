using Microsoft.Extensions.DependencyInjection;
using OneDesk.Models;
using OneDesk.Models.Tasks;
using OneDesk.Models.Tasks.Operations;
using OneDesk.Services.Clipboard;
using OneDesk.Services.Tasks;

namespace OneDesk.Services.FileCommand.Commands;

/// <summary>
/// 粘贴命令，用于将剪切板中的文件或文件夹粘贴到当前目录
/// </summary>
public class PasteCommand(IServiceProvider serviceProvider) : IFileCommand
{
    private IClipboardService ClipboardService => field ??= serviceProvider.GetRequiredService<IClipboardService>();
    private ITaskScheduler TaskScheduler => field ??= serviceProvider.GetRequiredService<ITaskScheduler>();
    private AppConfig Config => field ??= serviceProvider.GetRequiredService<AppConfig>();

    public string Name => "粘贴";

    public int Order => 22;

    public bool CanExecute(FileCommandContext context)
    {
        // 需要有当前文件夹且剪切板不为空才能执行
        return context.CurrentFolder != null && !ClipboardService.IsEmpty;
    }

    public async Task ExecuteAsync(FileCommandContext context)
    {
        if (!CanExecute(context))
            return;

        // 准备 ExtraData，传递 ConflictBehavior 配置
        var extraData = new Dictionary<string, object>
        {
            {
                "AdditionalData", new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", Enum.GetName(Config.ConflictBehavior)! }
                }
            }
        };

        // 根据剪切板模式选择操作类型
        ITaskOperation operation = ClipboardService.Mode == ClipboardMode.Copy
            ? CopyOperation.Instance
            : MoveOperation.Instance;

        // 为剪切板中的每个项目创建任务
        foreach (var item in ClipboardService.Items)
        {
            var taskInfo = new TaskInfo(context.UserInfo, operation, item, context.CurrentFolder, extraData);
            await TaskScheduler.AddTaskAsync(taskInfo);
        }

        // 清空剪切板
        ClipboardService.Clear();
    }
}
