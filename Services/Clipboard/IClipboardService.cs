using Microsoft.Graph.Models;

namespace OneDesk.Services.Clipboard;

/// <summary>
/// 剪切板操作模式
/// </summary>
public enum ClipboardMode
{
    /// <summary>
    /// 复制模式
    /// </summary>
    Copy,

    /// <summary>
    /// 剪切模式
    /// </summary>
    Cut
}

/// <summary>
/// 剪切板服务接口
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// 当前剪切板模式（复制或剪切）
    /// </summary>
    ClipboardMode Mode { get; }

    /// <summary>
    /// 剪切板中的项目列表
    /// </summary>
    IReadOnlyList<DriveItem> Items { get; }

    /// <summary>
    /// 剪切板是否为空
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// 设置剪切板内容
    /// </summary>
    /// <param name="mode">操作模式（复制或剪切）</param>
    /// <param name="items">要放入剪切板的项目列表</param>
    void SetClipboard(ClipboardMode mode, IEnumerable<DriveItem> items);

    /// <summary>
    /// 清空剪切板
    /// </summary>
    void Clear();
}
