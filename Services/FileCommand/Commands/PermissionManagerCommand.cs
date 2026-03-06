using Microsoft.Extensions.DependencyInjection;
using OneDesk.Views.Windows;

namespace OneDesk.Services.FileCommand.Commands;

/// <summary>
/// 权限管理命令，打开权限管理窗口
/// </summary>
public class PermissionManagerCommand(IServiceProvider serviceProvider) : IFileCommand
{
    private PermissionManagerWindow PermissionManagerWindow => field ??= serviceProvider.GetRequiredService<PermissionManagerWindow>();

    public string Name => "权限管理";

    public int Order => 110;

    public bool CanExecute(FileCommandContext context)
    {
        return context.SelectedItems.Count == 1;
    }

    public async Task ExecuteAsync(FileCommandContext context)
    {
        if (!CanExecute(context))
            return;

        var selectedItem = context.SelectedItems[0];

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            PermissionManagerWindow.ShowWithItem(selectedItem);
        });
    }
}
