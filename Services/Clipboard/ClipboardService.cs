using Microsoft.Graph.Models;

namespace OneDesk.Services.Clipboard;

/// <summary>
/// 剪切板服务实现
/// </summary>
public class ClipboardService : IClipboardService
{
    private readonly List<DriveItem> _items = [];

    public ClipboardMode Mode { get; private set; }

    public IReadOnlyList<DriveItem> Items => _items.AsReadOnly();

    public bool IsEmpty => _items.Count == 0;

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
}
