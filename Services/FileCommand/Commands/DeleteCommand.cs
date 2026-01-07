using Microsoft.Extensions.DependencyInjection;
using OneDesk.Helpers;
using OneDesk.Models.Tasks;
using OneDesk.Models.Tasks.Operations;
using OneDesk.Services.Tasks;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace OneDesk.Services.FileCommand.Commands;

/// <summary>
/// 删除命令，用于删除选中的文件或文件夹
/// </summary>
public class DeleteCommand(IServiceProvider serviceProvider) : IFileCommand
{
    private ITaskScheduler TaskScheduler => field ??= serviceProvider.GetRequiredService<ITaskScheduler>();

    public string Name => "删除";

    public int Order => 90;

    public bool CanExecute(FileCommandContext context)
    {
        // 至少选中一个项时才能执行
        return context.SelectedItems.Count > 0;
    }

    public async Task ExecuteAsync(FileCommandContext context)
    {
        if (!CanExecute(context))
        {
            return;
        }

        // 确认删除操作
        var itemCount = context.SelectedItems.Count;
        var itemNames = string.Join("、", context.SelectedItems.Take(3).Select(i => i.Name));
        var message = itemCount <= 3 ? $"确定要删除 {itemNames} 吗？" : $"确定要删除 {itemNames} 等 {itemCount} 个项吗？";

        var result = await CommonUtils.ShowMessageBoxAsync("确认删除", message, MsgButtonText.ConfirmCancel);

        if (result != MessageBoxResult.Primary)
        {
            return;
        }

        // 通过 TaskScheduler 为每个选中的项创建删除任务
        foreach (var item in context.SelectedItems)
        {
            var taskInfo = new TaskInfo(context.UserInfo, DeleteOperation.Instance, item);
            await TaskScheduler.AddTaskAsync(taskInfo);
        }
    }
}
