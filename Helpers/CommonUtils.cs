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

    public static async Task<string> ShowInputDialogAsync(string title, string message, string? defaultValue)
    {
        return await ShowInputDialogAsync(title, message, defaultValue, CancellationToken.None);
    }

    public static async Task<string> ShowInputDialogAsync(string title, string message, string? defaultValue, CancellationToken ct)
    {
        var buttonText = MsgButtonText.ConfirmCancel;
        defaultValue ??= "";
        return await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var stackPanel = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            // 提示信息
            var messageTextBox = new TextBox
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(messageTextBox);

            // 输入框
            var inputTextBox = new TextBox
            {
                Text = defaultValue,
                MinWidth = 280
            };
            stackPanel.Children.Add(inputTextBox);

            var messageBox = new MessageBox
            {
                Title = title,
                Content = stackPanel,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = buttonText.PrimaryText,
                IsSecondaryButtonEnabled = true,
                SecondaryButtonText = buttonText.SecondaryText,
                IsCloseButtonEnabled = false
            };

            // 设置焦点到输入框
            inputTextBox.Loaded += (s, e) =>
            {
                inputTextBox.Focus();
                inputTextBox.SelectAll();
            };

            var result = await messageBox.ShowDialogAsync(cancellationToken: ct);

            // 如果用户点击确认，返回输入的文本；否则返回 空字符串
            return result == MessageBoxResult.Primary ? inputTextBox.Text : "";
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
/// 消息框按钮文本
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