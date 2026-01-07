using Microsoft.Extensions.DependencyInjection;
using OneDesk.Services.Clipboard;

namespace OneDesk.Services.FileCommand.Commands;

/// <summary>
/// 复制命令，用于将选中的文件或文件夹复制到剪切板
/// </summary>
public class CopyCommand(IServiceProvider serviceProvider) : IFileCommand
{
    private IClipboardService ClipboardService => field ??= serviceProvider.GetRequiredService<IClipboardService>();

    public string Name => "复制";

    public int Order => 20;

    public bool CanExecute(FileCommandContext context)
    {
        // 至少选中一个项时才能执行
        return context.SelectedItems.Count > 0;
    }

    public Task ExecuteAsync(FileCommandContext context)
    {
        if (!CanExecute(context))
            return Task.CompletedTask;

        // 将选中的项目复制到剪切板
        ClipboardService.SetClipboard(ClipboardMode.Copy, context.SelectedItems);

        return Task.CompletedTask;
    }
}
