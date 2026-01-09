using ObservableCollections;

namespace OneDesk.Models.Tasks;

/// <summary>
/// 用户任务队列
/// </summary>
public partial class UserTaskQueue : ObservableObject, IDisposable
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo UserInfo { get; }

    /// <summary>
    /// 待处理任务队列（内部）
    /// </summary>
    private readonly ObservableQueue<TaskInfo> _pendingTasks = [];

    /// <summary>
    /// 运行中任务队列（内部）
    /// </summary>
    private readonly ObservableHashSet<TaskInfo> _runningTasks = [];

    /// <summary>
    /// 已完成任务列表（内部）
    /// </summary>
    private readonly ObservableList<TaskInfo> _completedTasks = [];

    /// <summary>
    /// 已取消任务列表（内部）
    /// </summary>
    private readonly ObservableList<TaskInfo> _cancelledTasks = [];

    /// <summary>
    /// 失败任务列表（内部）
    /// </summary>
    private readonly ObservableList<TaskInfo> _failedTasks = [];

    /// <summary>
    /// 待处理任务队列（只读视图）
    /// </summary>
    public NotifyCollectionChangedSynchronizedViewList<TaskInfo> PendingTasks { get; }

    /// <summary>
    /// 运行中任务队列（只读视图）
    /// </summary>
    public NotifyCollectionChangedSynchronizedViewList<TaskInfo> RunningTasks { get; }

    /// <summary>
    /// 已完成任务列表（只读视图）
    /// </summary>
    public NotifyCollectionChangedSynchronizedViewList<TaskInfo> CompletedTasks { get; }

    /// <summary>
    /// 已取消任务列表（只读视图）
    /// </summary>
    public NotifyCollectionChangedSynchronizedViewList<TaskInfo> CancelledTasks { get; }

    /// <summary>
    /// 失败任务列表（只读视图）
    /// </summary>
    public NotifyCollectionChangedSynchronizedViewList<TaskInfo> FailedTasks { get; }

    /// <summary>
    /// 当前查看的任务列表
    /// </summary>
    [ObservableProperty]
    private NotifyCollectionChangedSynchronizedViewList<TaskInfo>? _currentViewingTasks;

    /// <summary>
    /// 当前查看的任务列表标题
    /// </summary>
    [ObservableProperty]
    private string? _currentViewingTitle;

    /// <summary>
    /// 任务详情面板是否可见
    /// </summary>
    [ObservableProperty]
    private bool _isTaskDetailsPanelVisible;

    public UserTaskQueue(UserInfo userInfo)
    {
        UserInfo = userInfo;
        var current = SynchronizationContextCollectionEventDispatcher.Current;
        PendingTasks = _pendingTasks.ToNotifyCollectionChanged(current);
        RunningTasks = _runningTasks.ToNotifyCollectionChanged(current);
        CompletedTasks = _completedTasks.ToNotifyCollectionChangedSlim(current);
        CancelledTasks = _cancelledTasks.ToNotifyCollectionChangedSlim(current);
        FailedTasks = _failedTasks.ToNotifyCollectionChangedSlim(current);
    }

    /// <summary>
    /// 向待处理任务队列添加任务
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    public void AddPendingTask(TaskInfo taskInfo)
    {
        _pendingTasks.Enqueue(taskInfo);
    }

    /// <summary>
    /// 向已结束任务列表添加任务（根据任务状态自动分类）
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    public void AddCompletedTask(TaskInfo taskInfo)
    {
        _runningTasks.Remove(taskInfo);
        switch (taskInfo.Status)
        {
            case TaskStatus.Completed:
                _completedTasks.Add(taskInfo);
                break;
            case TaskStatus.Cancelled:
                _cancelledTasks.Add(taskInfo);
                break;
            case TaskStatus.Failed:
                _failedTasks.Add(taskInfo);
                break;
            case TaskStatus.Pending:
            case TaskStatus.Running:
            default:
                throw new InvalidOperationException($"无法将状态为 {taskInfo.Status} 的任务添加到已结束任务列表");
        }
    }

    /// <summary>
    /// 移除并获取待处理任务队列的第一个元素
    /// </summary>
    /// <returns>第一个待处理任务，如果队列为空则返回 null</returns>
    public TaskInfo? DequeuePendingTask()
    {
        if (!_pendingTasks.TryDequeue(out var task))
        {
            return null;
        }

        // 任务被取出准备执行，添加到运行中任务队列
        _runningTasks.Add(task);

        return task;
    }

    /// <summary>
    /// 清空所有已结束任务列表
    /// </summary>
    public void ClearCompletedTasks()
    {
        _completedTasks.Clear();
        _cancelledTasks.Clear();
        _failedTasks.Clear();
    }

    /// <summary>
    /// 切换查看指定状态的任务列表
    /// </summary>
    /// <param name="statusTag">状态标签（Pending, Running, Completed, Cancelled, Failed）</param>
    public void ToggleViewTasksByStatus(string statusTag)
    {
        // 获取对应状态的任务列表
        var taskList = statusTag switch
        {
            "Pending" => PendingTasks,
            "Running" => RunningTasks, // 运行中的任务没有单独的列表
            "Completed" => CompletedTasks,
            "Cancelled" => CancelledTasks,
            "Failed" => FailedTasks,
            _ => null
        };

        // 设置标题
        var title = statusTag switch
        {
            "Pending" => "待处理任务",
            "Running" => "运行中的任务",
            "Completed" => "已完成任务",
            "Cancelled" => "已取消任务",
            "Failed" => "失败任务",
            _ => "任务列表"
        };

        // 如果点击的是当前正在查看的状态，则收起面板
        if (IsTaskDetailsPanelVisible && ReferenceEquals(CurrentViewingTasks, taskList))
        {
            IsTaskDetailsPanelVisible = false;
            CurrentViewingTasks = null;
            CurrentViewingTitle = null;
            return;
        }

        // 更新当前查看的任务列表
        CurrentViewingTasks = taskList;

        // 显示标题
        CurrentViewingTitle = title;

        // 显示详情面板
        IsTaskDetailsPanelVisible = true;
    }

    public void Dispose()
    {
        PendingTasks.Dispose();
        RunningTasks.Dispose();
        CompletedTasks.Dispose();
        CancelledTasks.Dispose();
        FailedTasks.Dispose();
        GC.SuppressFinalize(this);
    }
}
