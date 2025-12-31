using Microsoft.Graph.Models;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace OneDesk.Helpers;

public class CommonUtils
{
    public static async Task ShowMessageBoxAsync(string title, string message)
    {
        await ShowMessageBoxAsync(title, message, CancellationToken.None);
    }

    public static async Task ShowMessageBoxAsync(string title, string message, CancellationToken ct)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var textBox = new TextBox
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true
            };
            var messageBox = new MessageBox
            {
                Title = title,
                Content = textBox
            };

            await messageBox.ShowDialogAsync(cancellationToken: ct);
        }).Task;
    }

    public static async Task<MessageBoxResult> ShowMessageBoxAsync(string title, string message, MsgButtonText buttonText)
    {
        return await ShowMessageBoxAsync(title, message, buttonText, CancellationToken.None);
    }

    public static async Task<MessageBoxResult> ShowMessageBoxAsync(string title, string message, MsgButtonText buttonText, CancellationToken ct)
    {
        return await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var textBox = new TextBox
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true
            };
            var messageBox = new MessageBox
            {
                Title = title,
                Content = textBox,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = buttonText.PrimaryText,
                IsSecondaryButtonEnabled = true,
                SecondaryButtonText = buttonText.SecondaryText,
                IsCloseButtonEnabled = false
            };

            return await messageBox.ShowDialogAsync(cancellationToken: ct);
        }).Task.Unwrap();
    }

    public static string GetDriveId(DriveItem item)
    {
        // 如果 ParentReference 为 null，则认为是远程项，使用 RemoteItem 的 ParentReference
        var parentReference = item.ParentReference is null ? item.RemoteItem!.ParentReference! : item.ParentReference!;
        var driveId = parentReference.DriveId!;
        return driveId;
    }
}

/// <summary>
/// 消息框按钮文本（类似枚举的结构体）
/// </summary>
public readonly struct MsgButtonText
{
    private MsgButtonText(string primaryText, string secondaryText)
    {
        PrimaryText = primaryText;
        SecondaryText = secondaryText;
    }

    /// <summary>
    /// 主按钮文本
    /// </summary>
    public string PrimaryText { get; }

    /// <summary>
    /// 次按钮文本
    /// </summary>
    public string SecondaryText { get; }

    /// <summary>
    /// 是、否
    /// </summary>
    public static readonly MsgButtonText YesNo = new("是", "否");

    /// <summary>
    /// 确认、取消
    /// </summary>
    public static readonly MsgButtonText ConfirmCancel = new("确认", "取消");
}