using System.IO;
using OneDesk.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Microsoft.Graph.Models;
using OneDesk.Models;

namespace OneDesk.Views.Pages;

public partial class FileManagerPage : INavigableView<FileManagerViewModel>
{
    public FileManagerViewModel ViewModel { get; }

    public FileManagerPage(FileManagerViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
        _ = ViewModel.GetCurrentPathChildren();
    }

    private void FileListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var selectedItem = FileListView.SelectedItem;
        if (selectedItem is not DriveItem item) return;
        if (item.Folder is null) return;
        var currentFolder = ViewModel.CurrentFolder;
        var newPath = Path.Combine(currentFolder.Path, item.Name!).Replace("\\","/");
        ViewModel.BreadcrumbItems.Add(new Item(item.Name!, newPath, currentFolder));
        _ = ViewModel.GetCurrentPathChildren();
    }
}