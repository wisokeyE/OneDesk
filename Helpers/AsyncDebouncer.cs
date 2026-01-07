namespace OneDesk.Helpers;

public class AsyncDebouncer: IDisposable
{
    private readonly int _delayMs;
    private readonly int _maxWaitMs;
    private readonly Func<Task>? _asyncAction;
    private readonly Action? _syncAction;
    private CancellationTokenSource? _delayCts;
    private CancellationTokenSource? _maxWaitCts;
    private bool _isWaiting;
    private bool _isExecuting;

    public AsyncDebouncer(int delayMs, int maxWaitMs, Func<Task> asyncAction)
    {
        _delayMs = delayMs;
        _maxWaitMs = maxWaitMs;
        _asyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
    }

    public AsyncDebouncer(int delayMs, int maxWaitMs, Action syncAction)
    {
        _delayMs = delayMs;
        _maxWaitMs = maxWaitMs;
        _syncAction = syncAction ?? throw new ArgumentNullException(nameof(syncAction));
    }

    public AsyncDebouncer(int delayMs, Func<Task> asyncAction): this(delayMs, 0, asyncAction)
    {
    }

    public AsyncDebouncer(int delayMs, Action syncAction): this(delayMs, 0, syncAction)
    {
    }

    public void Invoke()
    {
        lock (this)
        {
            if (_isExecuting) return;

            if (!_isWaiting)
            {
                _isWaiting = true;

                // 启动延迟任务
                _delayCts = new CancellationTokenSource();
                _ = StartTimer(_delayMs, _delayCts.Token);

                // 启动最大等待任务，如果设置了最大等待时间
                if (_maxWaitMs <= 0) return;
                _maxWaitCts = new CancellationTokenSource();
                _ = StartTimer(_maxWaitMs, _maxWaitCts.Token);
            }
            else
            {
                // 重置延迟任务
                _delayCts?.Cancel();
                _delayCts = new CancellationTokenSource();
                _ = StartTimer(_delayMs, _delayCts.Token);
            }
        }
    }

    private async Task StartTimer(int ms, CancellationToken token)
    {
        try
        {
            await Task.Delay(ms, token);
            if (token.IsCancellationRequested) return;
            await ExecuteAsync();
        }
        catch (TaskCanceledException) { }
    }

    private async Task ExecuteAsync()
    {
        lock (this)
        {
            if (!_isWaiting || _isExecuting) return;
            _isExecuting = true;

            // 取消所有计时任务
            _delayCts?.Cancel();
            _maxWaitCts?.Cancel();
        }

        try
        {
            if (_asyncAction != null)
            {
                await _asyncAction();
            }
            else
            {
                _syncAction?.Invoke();
            }
        }
        finally
        {
            lock (this)
            {
                _isWaiting = false;
                _isExecuting = false;
            }
        }
    }

    public void Dispose()
    {
        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _maxWaitCts?.Cancel();
        _maxWaitCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}