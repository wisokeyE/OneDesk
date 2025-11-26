using System.Diagnostics;
using Microsoft.Graph.Models;
using OneDesk.ViewModels.Windows;
using Wpf.Ui.Controls;

using Process = System.Diagnostics.Process;

namespace OneDesk.Views.Windows;

public partial class FileDetailsWindow : FluentWindow
{
    public FileDetailsWindowViewModel ViewModel { get; }

    public FileDetailsWindow(FileDetailsWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }

    public void ShowWithFileDetails(DriveItem item)
    {
        ViewModel.LoadFileDetails(item);
        ShowDialog();
    }

    private void WebUrlButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ViewModel.WebUrl) && ViewModel.IsWebUrlEnabled)
        {
            Process.Start(new ProcessStartInfo(ViewModel.WebUrl) { UseShellExecute = true });
        }
    }
}
