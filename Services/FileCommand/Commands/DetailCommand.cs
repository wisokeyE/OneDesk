using Microsoft.Extensions.DependencyInjection;
using OneDesk.Views.Windows;

namespace OneDesk.Services.FileCommand.Commands;

/// <summary>
/// 详情命令，用于显示文件或文件夹的详细信息
/// </summary>
public class DetailCommand(IServiceProvider serviceProvider) : IFileCommand
{
    public string Name => "详情";

    public int Order => 100;

    public bool CanExecute(FileCommandContext context)
    {
        // 只有选中单个项时才能执行
        return context.SelectedItems.Count == 1;
    }

    public async Task ExecuteAsync(FileCommandContext context)
    {
        if (!CanExecute(context))
            return;

        var selectedItem = context.SelectedItems[0];

        // 从服务容器获取 FileDetailsWindow
        var fileDetailsWindow = serviceProvider.GetRequiredService<FileDetailsWindow>();

        // 显示详情窗口
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            fileDetailsWindow.ShowWithFileDetails(selectedItem);
        });
    }
}
