using OneDesk.Helpers;

namespace OneDesk.Models.Tasks.Operations;

/// <summary>
/// 删除操作类（单例模式）
/// </summary>
public class DeleteOperation : ITaskOperation
{
    private static readonly Lazy<DeleteOperation> _instance = new(() => new DeleteOperation());

    /// <summary>
    /// 获取删除操作的单例实例
    /// </summary>
    public static DeleteOperation Instance => _instance.Value;

    private DeleteOperation()
    {
    }

    /// <summary>
    /// 操作名称
    /// </summary>
    public string OperationName => "删除";

    /// <summary>
    /// 执行删除操作
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ExecuteAsync(TaskInfo taskInfo, CancellationToken cancellationToken)
    {
        if (taskInfo.SourceItem.Id == null)
        {
            throw new InvalidOperationException("源项或源项 ID 不能为空");
        }

        var driveId = CommonUtils.GetDriveId(taskInfo.SourceItem);

        // 执行删除操作
        await taskInfo.UserInfo.Client.Drives[driveId].Items[taskInfo.SourceItem.Id].DeleteAsync(cancellationToken: cancellationToken);
    }
}
