namespace OneDesk.Models.Tasks;

/// <summary>
/// 任务操作接口
/// </summary>
public interface ITaskOperation
{
    /// <summary>
    /// 操作名称
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// 执行任务操作
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task ExecuteAsync(TaskInfo taskInfo, CancellationToken cancellationToken);
}
