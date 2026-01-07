using Microsoft.Graph.Models;
using OneDesk.Helpers;

namespace OneDesk.Models.Tasks.Operations;

/// <summary>
/// 新建文件夹操作类（单例模式）
/// </summary>
public class CreateFolderOperation : ITaskOperation
{
    private static readonly Lazy<CreateFolderOperation> _instance = new(() => new CreateFolderOperation());

    /// <summary>
    /// 获取新建文件夹操作的单例实例
    /// </summary>
    public static CreateFolderOperation Instance => _instance.Value;

    private CreateFolderOperation()
    {
    }

    private static readonly Dictionary<string, object> EmptyDictionary = [];

    /// <summary>
    /// 操作名称
    /// </summary>
    public string OperationName => "新建文件夹";

    /// <summary>
    /// 执行新建文件夹操作
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ExecuteAsync(TaskInfo taskInfo, CancellationToken cancellationToken)
    {
        // DestinationItem 存储父文件夹信息
        if (taskInfo.DestinationItem?.Id == null)
        {
            throw new InvalidOperationException("父文件夹不能为空");
        }

        // SourceItem 的 Name 属性存储新文件夹的名称
        if (string.IsNullOrWhiteSpace(taskInfo.SourceItem.Name))
        {
            throw new InvalidOperationException("文件夹名称不能为空");
        }

        var driveId = CommonUtils.GetDriveId(taskInfo.DestinationItem);

        // 创建新文件夹
        var newFolder = new DriveItem
        {
            Name = taskInfo.SourceItem.Name,
            Folder = new Folder(),
            AdditionalData = taskInfo.ExtraData?["AdditionalData"] as IDictionary<string, object> ?? EmptyDictionary
        };

        // 在父文件夹下创建新文件夹
        await taskInfo.UserInfo.Client.Drives[driveId]
            .Items[taskInfo.DestinationItem.Id]
            .Children
            .PostAsync(newFolder, cancellationToken: cancellationToken);
    }
}
