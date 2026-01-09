using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Threading.Channels;
using Nito.AsyncEx;
using OneDesk.Helpers;
using OneDesk.Models;
using OneDesk.Models.Tasks;
using OneDesk.Services.Auth;
using TaskStatus = OneDesk.Models.Tasks.TaskStatus;

namespace OneDesk.Services.Tasks;

/// <summary>
/// 任务调度器
/// </summary>
public class TaskScheduler : ITaskScheduler
{
    /// <summary>
    /// 全局任务队列（私有）
    /// </summary>
    private readonly Channel<TaskInfo> _globalQueue;

    /// <summary>
    /// 优先任务队列（私有）
    /// </summary>
    private readonly Channel<TaskInfo> _priorityQueue;

    /// <summary>
    /// 用户任务队列映射（用户ID -> UserTaskQueue）
    /// </summary>
    public ConcurrentDictionary<long, UserTaskQueue> UserQueues { get; } = [];

    /// <summary>
    /// 最大并发协程数量
    /// </summary>
    public int MaxConcurrentTasks => 5;

    /// <summary>
    /// 任务执行线程
    /// </summary>
    private readonly AsyncContextThread _asyncContextThread;

    /// <summary>
    /// 取消令牌源
    /// </summary>
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// UserInfoManager 引用
    /// </summary>
    private readonly IUserInfoManager _userInfoManager;

    /// <summary>
    /// 优先任务完成事件
    /// </summary>
    public event EventHandler? PriorityTaskCompleted;

    /// <summary>
    /// 用于控制优先任务完成事件触发频率的防抖器
    /// </summary>
    private readonly AsyncDebouncer _priorityTaskCompletedDebouncer;

    public TaskScheduler(IUserInfoManager userInfoManager)
    {
        _userInfoManager = userInfoManager;

        // 创建无界通道作为全局队列
        _globalQueue = Channel.CreateUnbounded<TaskInfo>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        // 创建无界通道作为优先队列
        _priorityQueue = Channel.CreateUnbounded<TaskInfo>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        _cancellationTokenSource = new CancellationTokenSource();

        // 初始化优先任务完成事件防抖器，延迟 300ms，最大等待 1000ms
        _priorityTaskCompletedDebouncer = new AsyncDebouncer(200, 1000, () =>
        {
            PriorityTaskCompleted?.Invoke(this, EventArgs.Empty);
        });

        // 创建 AsyncContextThread
        _asyncContextThread = new AsyncContextThread();

        // 初始化用户队列
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var userInfo in _userInfoManager.UserInfos)
            {
                var userQueue = new UserTaskQueue(userInfo);
                UserQueues.TryAdd(userInfo.UserId, userQueue);
                userInfo.TaskQueue = userQueue;
            }
        });

        // 监听用户变化
        if (_userInfoManager.UserInfos is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += OnUserInfosChanged;
        }

        // 启动消费者协程
        _asyncContextThread.Factory.Run(() =>
        {
            for (var i = 0; i < MaxConcurrentTasks; i++)
            {
                _ = ConsumerLoopAsync(i);
            }
        });

        // 启动优先队列消费者（独立线程）
        _ = Task.Run(PriorityConsumerLoopAsync);
    }

    /// <summary>
    /// 处理用户集合变化
    /// </summary>
    private void OnUserInfosChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 将处理逻辑调度到任务执行线程
        _asyncContextThread.Factory.Run(() =>
        {
            switch (e)
            {
                case { Action: NotifyCollectionChangedAction.Add, NewItems: not null }:
                    {
                        // 使用同步调度，确保更新用户队列时，不会有任务执行队列的并发修改问题
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (UserInfo userInfo in e.NewItems)
                            {
                                var userQueue = new UserTaskQueue(userInfo);
                                UserQueues.TryAdd(userInfo.UserId, userQueue);
                                userInfo.TaskQueue = userQueue;
                            }
                        });

                        break;
                    }
                case { Action: NotifyCollectionChangedAction.Remove, OldItems: not null }:
                    {
                        // 使用同步调度，确保更新用户队列时，不会有任务执行队列的并发修改问题
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (UserInfo userInfo in e.OldItems)
                            {
                                if (!UserQueues.TryRemove(userInfo.UserId, out var userQueue)) continue;
                                // 取消所有待处理任务
                                CancelAllPendingTasks(userQueue);
                                userInfo.TaskQueue = null; // 清理引用，将之前的循环引用断开
                            }
                        });

                        break;
                    }
            }
        });
    }

    /// <summary>
    /// 取消用户队列中的所有待处理任务
    /// </summary>
    private static void CancelAllPendingTasks(UserTaskQueue userQueue)
    {
        // 将所有待处理任务标记为已取消
        while (userQueue.DequeuePendingTask() is { } task)
        {
            task.Status = TaskStatus.Cancelled;
            task.EndTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 添加任务到队列
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    public async Task AddTaskAsync(TaskInfo taskInfo)
    {
        // 将添加任务的逻辑调度到任务执行线程
        await _asyncContextThread.Factory.Run(async () =>
        {
            var userId = taskInfo.UserInfo.UserId;
            // 获取用户队列
            if (!UserQueues.TryGetValue(userId, out var userQueue))
            {
                throw new InvalidOperationException($"用户 ID {userId} 的任务队列不存在");
            }

            // 添加到用户的待处理队列，使用同步调度，确保更新用户队列时，不会有任务执行队列的并发修改问题
            CommonUtils.RunOnUIThread(() => { userQueue.AddPendingTask(taskInfo); });

            // 添加到全局队列
            await _globalQueue.Writer.WriteAsync(taskInfo, _cancellationTokenSource.Token);
        });
    }

    /// <summary>
    /// 添加任务到优先队列
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    public async Task AddPriorityTaskAsync(TaskInfo taskInfo)
    {
        // 优先队列任务不放入用户队列，直接添加到优先队列
        await _priorityQueue.Writer.WriteAsync(taskInfo, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// 消费者循环，从队列中取任务并执行
    /// </summary>
    /// <param name="consumerId">消费者ID</param>
    private async Task ConsumerLoopAsync(int consumerId)
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // 从全局队列中读取任务
                var taskInfo = await _globalQueue.Reader.ReadAsync(_cancellationTokenSource.Token);
                var userId = taskInfo.UserInfo.UserId;

                // 如果用户队列不存在，任务无效，直接跳过
                if (!UserQueues.TryGetValue(userId, out var userQueue)) continue;

                // 从用户队列中移除，使用同步调度，确保更新用户队列时，不会有任务执行队列的并发修改问题
                CommonUtils.RunOnUIThread(() => { userQueue.DequeuePendingTask(); });

                // 执行任务
                await ExecuteTaskAsync(taskInfo);
            }
            catch (OperationCanceledException)
            {
                // 取消令牌被触发，退出循环
                break;
            }
            catch (Exception ex)
            {
                // 记录错误但继续运行
                System.Diagnostics.Debug.WriteLine($"消费者 {consumerId} 发生异常: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 执行任务的主体方法
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    /// <param name="addToCompletedQueue">是否将任务添加到已完成队列</param>
    private async Task ExecuteTaskAsync(TaskInfo taskInfo, bool addToCompletedQueue = true)
    {
        var userId = taskInfo.UserInfo.UserId;
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            // 检查任务状态
            var currentStatus = taskInfo.Status;
            if (currentStatus != TaskStatus.Pending && currentStatus != TaskStatus.Cancelled)
            {
                throw new InvalidOperationException($"任务状态异常：期望 Pending 或 Cancelled，实际为 {currentStatus}");
            }

            // 如果任务已被取消，直接移动到已结束队列（如果需要）
            if (currentStatus == TaskStatus.Cancelled)
            {
                if (addToCompletedQueue && UserQueues.TryGetValue(userId, out var cancelledUserQueue))
                {
                    await CommonUtils.RunOnUIThreadAsync(() => { cancelledUserQueue.AddCompletedTask(taskInfo); });
                }
                return;
            }

            // 更新任务状态为运行中
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                taskInfo.Status = TaskStatus.Running;
                taskInfo.StartTime = DateTime.Now;
            });

            // 执行任务操作
            await taskInfo.Operation.ExecuteAsync(taskInfo, cancellationToken);

            // 任务成功完成
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                taskInfo.Status = TaskStatus.Completed;
                taskInfo.EndTime = DateTime.Now;
                taskInfo.Progress = taskInfo.Progress > 0 ? taskInfo.Progress : 100;
            });
        }
        catch (OperationCanceledException)
        {
            // 任务被取消
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                taskInfo.Status = TaskStatus.Cancelled;
                taskInfo.EndTime = DateTime.Now;
            });
        }
        catch (Exception ex)
        {
            // 任务失败
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                taskInfo.Status = TaskStatus.Failed;
                taskInfo.EndTime = DateTime.Now;
                taskInfo.FailureMessage = ex.Message;
            });
        }
        finally
        {
            // 将任务移动到已结束队列（如果需要）
            if (addToCompletedQueue && UserQueues.TryGetValue(userId, out var userQueue))
            {
                await CommonUtils.RunOnUIThreadAsync(() => { userQueue.AddCompletedTask(taskInfo); });
            }
        }
    }

    /// <summary>
    /// 优先队列消费者循环，专门处理优先任务
    /// </summary>
    private async Task PriorityConsumerLoopAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // 从优先队列中读取任务
                var taskInfo = await _priorityQueue.Reader.ReadAsync(_cancellationTokenSource.Token);
                var userId = taskInfo.UserInfo.UserId;

                // 如果用户队列不存在，任务无效，直接跳过
                if (!UserQueues.TryGetValue(userId, out _)) continue;

                // 执行优先任务（不放入已完成队列）
                await ExecuteTaskAsync(taskInfo, false);

                // 任务完成后通过防抖器触发事件
                _priorityTaskCompletedDebouncer.Invoke();
            }
            catch (OperationCanceledException)
            {
                // 取消令牌被触发，退出循环
                break;
            }
            catch (Exception ex)
            {
                // 记录错误但继续运行
                System.Diagnostics.Debug.WriteLine($"优先队列消费者发生异常: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 取消订阅
        if (_userInfoManager.UserInfos is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= OnUserInfosChanged;
        }

        _cancellationTokenSource.Cancel();
        _globalQueue.Writer.Complete();
        _priorityQueue.Writer.Complete();

        // 释放 AsyncContextThread
        _asyncContextThread.Join();

        // 释放防抖器
        _priorityTaskCompletedDebouncer.Dispose();

        // 释放用户队列
        foreach (var (_, userQueue) in UserQueues)
        {
            userQueue.Dispose();
        }

        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}
