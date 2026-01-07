using Microsoft.Graph.Models;
using OneDesk.Services.Auth;

namespace OneDesk.Services.Clipboard;

/// <summary>
/// 剪切板服务实现
/// </summary>
public class ClipboardService : IClipboardService, IDisposable
{
    private readonly IUserInfoManager _userInfoManager;
    private readonly List<DriveItem> _items = [];

    public ClipboardMode Mode { get; private set; }

    public IReadOnlyList<DriveItem> Items => _items.AsReadOnly();

    public bool IsEmpty => _items.Count == 0;

    public ClipboardService(IUserInfoManager userInfoManager)
    {
        _userInfoManager = userInfoManager;
        // 监听用户切换事件，当用户切换时清空剪切板
        _userInfoManager.PropertyChanged += OnUserInfoManagerPropertyChanged;
    }

    public void SetClipboard(ClipboardMode mode, IEnumerable<DriveItem> items)
    {
        Mode = mode;
        _items.Clear();
        _items.AddRange(items);
    }

    public void Clear()
    {
        _items.Clear();
        Mode = ClipboardMode.Copy; // 重置为默认模式
    }

    private void OnUserInfoManagerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 当激活的用户信息发生变化时，清空剪切板
        if (e.PropertyName == nameof(IUserInfoManager.ActivatedUserInfo))
        {
            Clear();
        }
    }

    public void Dispose()
    {
        _userInfoManager.PropertyChanged -= OnUserInfoManagerPropertyChanged;
        GC.SuppressFinalize(this);
    }
}
