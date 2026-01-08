using Microsoft.Extensions.DependencyInjection;
using OneDesk.Helpers;
using OneDesk.Models;
using OneDesk.Models.Tasks;
using OneDesk.Models.Tasks.Operations;
using OneDesk.Services.Tasks;

namespace OneDesk.Services.FileCommand.Commands;

/// <summary>
/// 重命名命令，用于重命名选中的文件或文件夹
/// </summary>
public class RenameCommand(IServiceProvider serviceProvider) : IFileCommand
{
    private ITaskScheduler TaskScheduler => field ??= serviceProvider.GetRequiredService<ITaskScheduler>();

    private AppConfig Config => field ??= serviceProvider.GetRequiredService<AppConfig>();

    public string Name => "重命名";

    public int Order => 30;

    public bool CanExecute(FileCommandContext context)
    {
        // 只能重命名单个文件或文件夹
        return context.SelectedItems.Count == 1;
    }

    public async Task ExecuteAsync(FileCommandContext context)
    {
        if (!CanExecute(context))
        {
            return;
        }

        var selectedItem = context.SelectedItems[0];
        var currentName = selectedItem.Name ?? string.Empty;

        // 弹出输入对话框，要求用户输入新名称
        var newName = await CommonUtils.ShowInputDialogAsync(
            "重命名",
            "请输入新名称：",
            currentName
        );

        // 用户取消或输入为空
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        // 如果名称没有变化，直接返回
        if (newName == currentName)
        {
            return;
        }

        // 创建额外数据，传递新名称
        var extraData = new Dictionary<string, object>
        {
            { "NewName", newName },
            {
                "AdditionalData", new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", Enum.GetName(Config.ConflictBehavior)! }
                }
            }
        };

        // 创建任务，复用 MoveOperation
        // SourceItem 为要重命名的项，DestinationItem 为其父文件夹（保持在原位置）
        var taskInfo = new TaskInfo(context.UserInfo, MoveOperation.Instance, selectedItem, context.CurrentFolder, extraData);

        await TaskScheduler.AddPriorityTaskAsync(taskInfo);
    }
}
