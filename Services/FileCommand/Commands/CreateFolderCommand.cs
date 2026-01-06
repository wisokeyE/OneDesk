using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;
using OneDesk.Helpers;
using OneDesk.Models.Tasks;
using OneDesk.Models.Tasks.Operations;
using OneDesk.Services.Tasks;

namespace OneDesk.Services.FileCommand.Commands;

/// <summary>
/// 新建文件夹命令，用于在当前目录下创建新文件夹
/// </summary>
public class CreateFolderCommand(IServiceProvider serviceProvider) : IFileCommand
{
    public string Name => "新建文件夹";

    public int Order => 10;

    public bool CanExecute(FileCommandContext context)
    {
        // 需要有当前文件夹才能执行
        return context.CurrentFolder != null;
    }

    public async Task ExecuteAsync(FileCommandContext context)
    {
        if (!CanExecute(context))
        {
            return;
        }

        // 弹出输入对话框，要求用户输入文件夹名称
        var folderName = await CommonUtils.ShowInputDialogAsync(
            "新建文件夹",
            "请输入文件夹名称：",
            "新建文件夹"
        );

        // 用户取消或输入为空
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return;
        }

        // 从服务容器获取 TaskScheduler
        var taskScheduler = serviceProvider.GetRequiredService<ITaskScheduler>();

        // 创建一个临时的 DriveItem 来存储文件夹名称
        var newFolderItem = new DriveItem
        {
            Name = folderName
        };

        // 创建任务，DestinationItem 为父文件夹，SourceItem 存储新文件夹名称
        var taskInfo = new TaskInfo(context.UserInfo, CreateFolderOperation.Instance, newFolderItem, context.CurrentFolder);

        await taskScheduler.AddTaskAsync(taskInfo);
    }
}
