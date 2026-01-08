using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Drives.Item.Items.Item.Copy;
using Microsoft.Graph.Models;
using OneDesk.Helpers;
using OneDesk.Services.Tasks;

namespace OneDesk.Models.Tasks.Operations;

/// <summary>
/// 复制操作类（单例模式）
/// </summary>
public class CopyOperation : ITaskOperation
{
    private static readonly Lazy<CopyOperation> _instance = new(() => new CopyOperation());

    /// <summary>
    /// 获取复制操作的单例实例
    /// </summary>
    public static CopyOperation Instance => _instance.Value;

    private CopyOperation()
    {
    }

    private static readonly Dictionary<string, object> EmptyDictionary = [];

    private ITaskScheduler TaskScheduler => field ??= App.Services.GetRequiredService<ITaskScheduler>();

    /// <summary>
    /// 操作名称
    /// </summary>
    public string OperationName => "复制";

    /// <summary>
    /// 执行复制操作
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ExecuteAsync(TaskInfo taskInfo, CancellationToken cancellationToken)
    {
        if (taskInfo.SourceItem.Id == null)
        {
            throw new InvalidOperationException("源项或源项 ID 不能为空");
        }

        if (taskInfo.DestinationItem?.Id == null)
        {
            throw new InvalidOperationException("目标项或目标项 ID 不能为空");
        }

        var sourceDriveId = CommonUtils.GetDriveId(taskInfo.SourceItem);
        var destinationDriveId = CommonUtils.GetDriveId(taskInfo.DestinationItem);

        // 判断源是文件还是文件夹
        if (taskInfo.SourceItem.Folder != null)
        {
            // 源是文件夹，需要递归复制
            await CopyFolderAsync(taskInfo, sourceDriveId, destinationDriveId, cancellationToken);
        }
        else
        {
            // 源是文件，直接复制
            await CopyFileAsync(taskInfo, destinationDriveId, cancellationToken);
        }
    }

    /// <summary>
    /// 复制文件
    /// </summary>
    private static async Task CopyFileAsync(TaskInfo taskInfo, string destinationDriveId, CancellationToken cancellationToken)
    {
        // 构造目标父文件夹引用
        var parentReference = new ItemReference
        {
            DriveId = destinationDriveId,
            Id = taskInfo.DestinationItem!.Id
        };

        // 执行复制操作
        var client = taskInfo.UserInfo.Client;
        await client.Drives[destinationDriveId]
            .Items[taskInfo.SourceItem.Id!]
            .Copy
            .PostAsync(new CopyPostRequestBody
            {
                ParentReference = parentReference,
                Name = taskInfo.SourceItem.Name,
                AdditionalData = CommonUtils.GetValueOrDefault(taskInfo.ExtraData, "AdditionalData", EmptyDictionary)
            }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 复制文件夹
    /// </summary>
    private async Task CopyFolderAsync(TaskInfo taskInfo, string sourceDriveId, string destinationDriveId, CancellationToken cancellationToken)
    {
        // 检查目标下是否存在同名文件夹
        var client = taskInfo.UserInfo.Client;
        var children = await client.Drives[destinationDriveId]
            .Items[taskInfo.DestinationItem!.Id!]
            .Children
            .GetAsync(cancellationToken: cancellationToken);

        var existingFolder = children?.Value?.FirstOrDefault(item =>
            item.Name == taskInfo.SourceItem.Name && item.Folder != null);

        DriveItem targetFolder;

        if (existingFolder == null)
        {
            // 目标下不存在同名文件夹，创建新文件夹
            var newFolder = new DriveItem
            {
                Name = taskInfo.SourceItem.Name,
                Folder = new Folder()
            };

            targetFolder = await client.Drives[destinationDriveId]
                .Items[taskInfo.DestinationItem.Id!]
                .Children
                .PostAsync(newFolder, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("创建目标文件夹失败");
        }
        else
        {
            // 目标下已存在同名文件夹，使用现有文件夹
            targetFolder = existingFolder;
        }

        // 获取源文件夹的所有子项
        var sourceChildren = await client.Drives[sourceDriveId]
            .Items[taskInfo.SourceItem.Id!]
            .Children
            .GetAsync(cancellationToken: cancellationToken);

        if (sourceChildren?.Value is not { Count: not 0 })
        {
            // 源文件夹为空，无需复制子项
            return;
        }

        // 遍历源文件夹的所有子项，为每个子项创建复制任务
        foreach (var childItem in sourceChildren.Value)
        {
            var childTaskInfo = new TaskInfo(taskInfo.UserInfo, Instance, childItem, targetFolder, taskInfo.ExtraData);
            await TaskScheduler.AddTaskAsync(childTaskInfo);
        }
    }
}
