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
}
