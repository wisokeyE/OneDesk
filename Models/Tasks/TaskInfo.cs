using Microsoft.Graph.Models;

namespace OneDesk.Models.Tasks;

/// <summary>
/// 任务状态枚举
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Pending,

    /// <summary>
    /// 运行中
    /// </summary>
    Running,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled,

    /// <summary>
    /// 失败
    /// </summary>
    Failed
}

/// <summary>
/// 任务信息类
/// </summary>
public partial class TaskInfo(UserInfo userInfo, ITaskOperation taskOperation, DriveItem sourceItem, DriveItem? destinationItem, Dictionary<string, object>? extraData) : ObservableObject
{
    private static long _nextId;

    /// <summary>
    /// 递增Id，应用不重启期间唯一标识任务
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// 用户信息
    /// </summary>
    [ObservableProperty]
    private UserInfo _userInfo = userInfo;

    /// <summary>
    /// 创建时间
    /// </summary>
    [ObservableProperty]
    private DateTime _createdTime = DateTime.Now;

    /// <summary>
    /// 开始时间
    /// </summary>
    [ObservableProperty]
    private DateTime? _startTime;

    /// <summary>
    /// 结束时间
    /// </summary>
    [ObservableProperty]
    private DateTime? _endTime;

    /// <summary>
    /// 任务进度（0.0 到 100.0）
    /// </summary>
    [ObservableProperty]
    private double _progress;

    /// <summary>
    /// 任务状态
    /// </summary>
    [ObservableProperty]
    private TaskStatus _status = TaskStatus.Pending;

    /// <summary>
    /// 任务操作
    /// </summary>
    [ObservableProperty]
    private ITaskOperation _operation = taskOperation;

    /// <summary>
    /// 源项
    /// </summary>
    [ObservableProperty]
    private DriveItem _sourceItem = sourceItem;

    /// <summary>
    /// 目标项（可为空）
    /// </summary>
    [ObservableProperty]
    private DriveItem? _destinationItem = destinationItem;

    /// <summary>
    /// 失败信息（仅在任务失败时有值）
    /// </summary>
    [ObservableProperty]
    private string? _failureMessage;

    /// <summary>
    /// 额外数据（用于传输额外信息）
    /// </summary>
    [ObservableProperty]
    private Dictionary<string, object>? _extraData = extraData;

    public TaskInfo(UserInfo userInfo, ITaskOperation taskOperation, DriveItem sourceItem)
        : this(userInfo, taskOperation, sourceItem, null, null)
    {
        Id = Interlocked.Increment(ref _nextId);
    }
}
