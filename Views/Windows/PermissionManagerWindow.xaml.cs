using Microsoft.Graph.Models;
using OneDesk.ViewModels.Windows;
using Wpf.Ui.Controls;

namespace OneDesk.Views.Windows;

public partial class PermissionManagerWindow : FluentWindow
{
    public PermissionManagerWindowViewModel ViewModel { get; }

    public PermissionManagerWindow(PermissionManagerWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();

        Closing += (s, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }

    public void ShowWithItem(DriveItem item)
    {
        ViewModel.LoadItem(item);
        Show();
    }
}
