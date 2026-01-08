using System.Collections.Concurrent;
using OneDesk.Models.Tasks;

namespace OneDesk.Services.Tasks;

/// <summary>
/// 任务调度器接口
/// </summary>
public interface ITaskScheduler : IDisposable
{
    /// <summary>
    /// 用户任务队列映射（用户ID -> UserTaskQueue）
    /// </summary>
    ConcurrentDictionary<int, UserTaskQueue> UserQueues { get; }

    /// <summary>
    /// 最大并发协程数量
    /// </summary>
    int MaxConcurrentTasks { get; }

    /// <summary>
    /// 添加任务到队列
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    Task AddTaskAsync(TaskInfo taskInfo);

    /// <summary>
    /// 添加任务到优先队列
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    Task AddPriorityTaskAsync(TaskInfo taskInfo);

    /// <summary>
    /// 优先任务完成事件
    /// </summary>
    event EventHandler? PriorityTaskCompleted;
}
