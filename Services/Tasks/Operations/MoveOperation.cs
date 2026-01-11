using Microsoft.Graph.Models;
using OneDesk.Helpers;
using OneDesk.Models.Tasks;

namespace OneDesk.Services.Tasks.Operations;

/// <summary>
/// 移动操作类（单例模式）
/// </summary>
public class MoveOperation : ITaskOperation
{
    private static readonly Dictionary<string, object> EmptyDictionary = [];

    /// <summary>
    /// 操作名称
    /// </summary>
    public string OperationName => "移动";

    /// <summary>
    /// 执行移动操作
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

        // 构造目标父文件夹引用
        var parentReference = new ItemReference
        {
            DriveId = destinationDriveId,
            Id = taskInfo.DestinationItem.Id
        };

        // 执行移动操作（通过 PATCH 更新 parentReference）
        var updateItem = new DriveItem
        {
            ParentReference = parentReference,
            // 如果 ExtraData 中有 NewName，使用新名称，否则使用原名称
            Name = CommonUtils.GetValueOrDefault(taskInfo.ExtraData, "NewName", taskInfo.SourceItem.Name),
            AdditionalData = CommonUtils.GetValueOrDefault(taskInfo.ExtraData, "AdditionalData", EmptyDictionary)
        };

        await taskInfo.UserInfo.Client.Drives[sourceDriveId]
            .Items[taskInfo.SourceItem.Id]
            .PatchAsync(updateItem, cancellationToken: cancellationToken);
    }
}
