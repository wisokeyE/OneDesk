using System.Collections.ObjectModel;

namespace OneDesk.Models.Tasks;

/// <summary>
/// 用户任务队列
/// </summary>
public partial class UserTaskQueue : ObservableObject
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo UserInfo { get; }

    /// <summary>
    /// 待处理任务列表（内部）
    /// </summary>
    private readonly ObservableCollection<TaskInfo> _pendingTasks = [];

    /// <summary>
    /// 已完成任务列表（内部）
    /// </summary>
    private readonly ObservableCollection<TaskInfo> _completedTasks = [];

    /// <summary>
    /// 已取消任务列表（内部）
    /// </summary>
    private readonly ObservableCollection<TaskInfo> _cancelledTasks = [];

    /// <summary>
    /// 失败任务列表（内部）
    /// </summary>
    private readonly ObservableCollection<TaskInfo> _failedTasks = [];

    /// <summary>
    /// 正在执行的任务数量
    /// </summary>
    public int RunningTaskCount
    {
        get;
        private set
        {
            OnPropertyChanging();
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 待处理任务列表（只读）
    /// </summary>
    public ReadOnlyObservableCollection<TaskInfo> PendingTasks { get; }

    /// <summary>
    /// 已完成任务列表（只读）
    /// </summary>
    public ReadOnlyObservableCollection<TaskInfo> CompletedTasks { get; }

    /// <summary>
    /// 已取消任务列表（只读）
    /// </summary>
    public ReadOnlyObservableCollection<TaskInfo> CancelledTasks { get; }

    /// <summary>
    /// 失败任务列表（只读）
    /// </summary>
    public ReadOnlyObservableCollection<TaskInfo> FailedTasks { get; }

    /// <summary>
    /// 当前查看的任务列表
    /// </summary>
    [ObservableProperty]
    private ReadOnlyObservableCollection<TaskInfo>? _currentViewingTasks;

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
        PendingTasks = new ReadOnlyObservableCollection<TaskInfo>(_pendingTasks);
        CompletedTasks = new ReadOnlyObservableCollection<TaskInfo>(_completedTasks);
        CancelledTasks = new ReadOnlyObservableCollection<TaskInfo>(_cancelledTasks);
        FailedTasks = new ReadOnlyObservableCollection<TaskInfo>(_failedTasks);
    }

    /// <summary>
    /// 向待处理任务列表添加任务
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    public void AddPendingTask(TaskInfo taskInfo)
    {
        _pendingTasks.Add(taskInfo);
    }

    /// <summary>
    /// 向已结束任务列表添加任务（根据任务状态自动分类）
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    public void AddCompletedTask(TaskInfo taskInfo)
    {
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
        RunningTaskCount--;
    }

    /// <summary>
    /// 移除并获取待处理任务列表的第一个元素
    /// </summary>
    /// <returns>第一个待处理任务，如果列表为空则返回 null</returns>
    public TaskInfo? DequeuePendingTask()
    {
        if (_pendingTasks.Count == 0)
        {
            return null;
        }

        var task = _pendingTasks[0];
        _pendingTasks.RemoveAt(0);

        // 任务被取出准备执行，增加运行计数
        RunningTaskCount++;

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
            "Running" => null, // 运行中的任务没有单独的列表
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
        if (IsTaskDetailsPanelVisible && CurrentViewingTasks == taskList)
        {
            IsTaskDetailsPanelVisible = false;
            CurrentViewingTasks = null;
            CurrentViewingTitle = null;
            return;
        }

        // 更新当前查看的任务列表
        CurrentViewingTasks = taskList;

        // 显示任务总数
        CurrentViewingTitle = title;

        // 显示详情面板
        IsTaskDetailsPanelVisible = true;
    }
}
