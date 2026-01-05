using System.Windows.Controls;
using OneDesk.Models;
using OneDesk.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace OneDesk.Views.Pages;

/// <summary>
/// 任务管理页面
/// </summary>
public partial class TaskManagerPage : INavigableView<TaskManagerViewModel>
{
    public TaskManagerViewModel ViewModel { get; }

    public TaskManagerPage(TaskManagerViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    /// <summary>
    /// 处理任务状态卡片点击事件
    /// </summary>
    private void OnTaskStatusClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Border clickedBorder)
            return;

        // 获取状态类型
        var statusTag = clickedBorder.Tag as string;
        if (string.IsNullOrEmpty(statusTag))
            return;

        // 获取用户信息（DataContext）
        var userInfo = clickedBorder.DataContext as UserInfo;

        // 调用 UserTaskQueue 的方法切换查看状态
        userInfo?.TaskQueue?.ToggleViewTasksByStatus(statusTag);
    }
}
