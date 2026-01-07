using Microsoft.Extensions.DependencyInjection;
using OneDesk.Services.Clipboard;

namespace OneDesk.Services.FileCommand.Commands;

/// <summary>
/// 剪切命令，用于将选中的文件或文件夹剪切到剪切板
/// </summary>
public class CutCommand(IServiceProvider serviceProvider) : IFileCommand
{
    private IClipboardService ClipboardService => field ??= serviceProvider.GetRequiredService<IClipboardService>();

    public string Name => "剪切";

    public int Order => 21;

    public bool CanExecute(FileCommandContext context)
    {
        // 至少选中一个项时才能执行
        return context.SelectedItems.Count > 0;
    }

    public Task ExecuteAsync(FileCommandContext context)
    {
        if (!CanExecute(context))
            return Task.CompletedTask;

        // 将选中的项目剪切到剪切板
        ClipboardService.SetClipboard(ClipboardMode.Cut, context.SelectedItems);

        return Task.CompletedTask;
    }
}
