using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using MessageBox = Wpf.Ui.Controls.MessageBox;

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
}