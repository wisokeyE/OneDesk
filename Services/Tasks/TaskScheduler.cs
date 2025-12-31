using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Threading.Channels;
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
    /// 用户任务队列映射（用户ID -> UserTaskQueue）
    /// </summary>
    public ConcurrentDictionary<int, UserTaskQueue> UserQueues { get; } = new();

    /// <summary>
    /// 最大并发协程数量
    /// </summary>
    public int MaxConcurrentTasks { get; } = 5;

    /// <summary>
    /// 任务执行线程
    /// </summary>
    private readonly Thread _executorThread;

    /// <summary>
    /// 取消令牌源
    /// </summary>
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// 队列访问锁
    /// </summary>
    private readonly SemaphoreSlim _queueLock = new(1, 1);

    /// <summary>
    /// 并发任务信号量（限制同时执行的任务数量）
    /// </summary>
    private readonly SemaphoreSlim _concurrencyLimiter;

    /// <summary>
    /// UserInfoManager 引用
    /// </summary>
    private readonly IUserInfoManager _userInfoManager;

    public TaskScheduler(IUserInfoManager userInfoManager)
    {
        _userInfoManager = userInfoManager;

        // 创建无界通道作为全局队列
        _globalQueue = Channel.CreateUnbounded<TaskInfo>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        _cancellationTokenSource = new CancellationTokenSource();
        _concurrencyLimiter = new SemaphoreSlim(MaxConcurrentTasks, MaxConcurrentTasks);

        // 初始化用户队列
        foreach (var userInfo in _userInfoManager.UserInfos)
        {
            var userQueue = new UserTaskQueue(userInfo);
            UserQueues.TryAdd(userInfo.UserId, userQueue);
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                userInfo.TaskQueue = userQueue;
            });
        }

        // 监听用户变化
        if (_userInfoManager.UserInfos is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += OnUserInfosChanged;
        }

        // 创建独立线程运行任务执行器
        _executorThread = new Thread(ExecutorThreadMain)
        {
            Name = "TaskExecutorThread",
            IsBackground = true
        };
        _executorThread.Start();
    }

    /// <summary>
    /// 处理用户集合变化
    /// </summary>
    private void OnUserInfosChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e)
        {
            case { Action: NotifyCollectionChangedAction.Add, NewItems: not null }:
            {
                foreach (UserInfo userInfo in e.NewItems)
                {
                    var userQueue = new UserTaskQueue(userInfo);
                    UserQueues.TryAdd(userInfo.UserId, userQueue);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        userInfo.TaskQueue = userQueue;
                    });
                }

                break;
            }
            case { Action: NotifyCollectionChangedAction.Remove, OldItems: not null }:
            {
                foreach (UserInfo userInfo in e.OldItems)
                {
                    if (UserQueues.TryRemove(userInfo.UserId, out var userQueue))
                    {
                        // 取消所有待处理任务
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CancelAllPendingTasks(userQueue);
                            userInfo.TaskQueue = null; // 清理引用，将之前的循环引用断开
                        });
                    }
                }

                break;
            }
        }
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
        var userId = taskInfo.UserInfo.UserId;
        // 获取用户队列
        if (!UserQueues.TryGetValue(userId, out var userQueue))
        {
            throw new InvalidOperationException($"用户 ID {userId} 的任务队列不存在");
        }

        // 添加到用户的待处理队列
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            userQueue.AddPendingTask(taskInfo);
        });

        // 添加到全局队列
        await _globalQueue.Writer.WriteAsync(taskInfo, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// 取出下一个待执行的任务（带锁保护）
    /// </summary>
    /// <returns>任务信息，如果取消则返回 null</returns>
    private async Task<TaskInfo?> DequeueTaskAsync()
    {
        await _queueLock.WaitAsync(_cancellationTokenSource.Token);
        try
        {
            var taskInfo = await _globalQueue.Reader.ReadAsync(_cancellationTokenSource.Token);

            var userId = taskInfo.UserInfo.UserId;

            // 从用户队列中移除
            if (UserQueues.TryGetValue(userId, out var userQueue))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    userQueue.DequeuePendingTask();
                });
            }
            else
            {
                // 用户队列不存在，任务无效
                return null;
            }

            return taskInfo;
        }
        finally
        {
            _queueLock.Release();
        }
    }

    /// <summary>
    /// 执行任务的主体方法
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    private async Task ExecuteTaskAsync(TaskInfo taskInfo)
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

            // 如果任务已被取消，直接移动到已结束队列
            if (currentStatus == TaskStatus.Cancelled)
            {
                if (UserQueues.TryGetValue(userId, out var cancelledUserQueue))
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        cancelledUserQueue.AddCompletedTask(taskInfo);
                    });
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
            // 将任务移动到已结束队列
            if (UserQueues.TryGetValue(userId, out var userQueue))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    userQueue.AddCompletedTask(taskInfo);
                });
            }
        }
    }

    /// <summary>
    /// 执行器线程主函数
    /// </summary>
    private void ExecutorThreadMain()
    {
        var tasks = new List<Task>();

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // 等待有可用的并发槽位
                _concurrencyLimiter.Wait(_cancellationTokenSource.Token);

                // 取出任务
                var dequeueTask = DequeueTaskAsync();
                dequeueTask.Wait(_cancellationTokenSource.Token);
                var taskInfo = dequeueTask.Result;

                if (taskInfo != null)
                {
                    // 启动新的协程执行任务
                    var executeTask = Task.Run(async () =>
                    {
                        try
                        {
                            await ExecuteTaskAsync(taskInfo);
                        }
                        finally
                        {
                            // 释放并发槽位
                            _concurrencyLimiter.Release();
                        }
                    }, _cancellationTokenSource.Token);

                    tasks.Add(executeTask);

                    // 清理已完成的任务
                    tasks.RemoveAll(t => t.IsCompleted);
                }
                else
                {
                    // 没有任务，释放槽位
                    _concurrencyLimiter.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // 记录错误但继续运行
                _concurrencyLimiter.Release();
            }
        }

        // 等待所有任务完成
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));
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
        _executorThread.Join(TimeSpan.FromSeconds(5));
        _cancellationTokenSource.Dispose();
        _queueLock.Dispose();
        _concurrencyLimiter.Dispose();
        GC.SuppressFinalize(this);
    }
}
