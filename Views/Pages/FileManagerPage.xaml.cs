using System.IO;
using System.Windows.Controls;
using Microsoft.Graph.Models;
using OneDesk.Models;
using OneDesk.ViewModels.Pages;
using OneDesk.Views.Windows;
using Wpf.Ui.Abstractions.Controls;
using ListView = Wpf.Ui.Controls.ListView;

namespace OneDesk.Views.Pages;

public partial class FileManagerPage : INavigableView<FileManagerViewModel>
{
    public FileManagerViewModel ViewModel { get; }
    private readonly FileDetailsWindow _fileDetailsWindow;

    public FileManagerPage(FileManagerViewModel viewModel, FileDetailsWindow fileDetailsWindow)
    {
        ViewModel = viewModel;
        _fileDetailsWindow = fileDetailsWindow;
        DataContext = this;

        InitializeComponent();
        _ = ViewModel.GetCurrentPathChildren();
    }

    private void FileListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var selectedItem = FileListView.SelectedItem;
        if (selectedItem is not DriveItem item) return;

        // 如果是文件夹，进入文件夹
        if (item.Folder is not null)
        {
            var currentFolder = ViewModel.CurrentFolder;
            var newPath = Path.Combine(currentFolder.Path, item.Name!).Replace("\\","/");
            ViewModel.BreadcrumbItems.Add(new Item(item.Name!, newPath, currentFolder, item));
            _ = ViewModel.GetCurrentPathChildren();
        }
        // 如果是文件，显示详细信息窗口
        else
        {
            _fileDetailsWindow.Owner = Window.GetWindow(this);
            _fileDetailsWindow.ShowWithFileDetails(item);
        }
    }

    public void OnRootIndexChanged(object sender, SelectionChangedEventArgs args)
    {
        if (sender is not ListView rootListView) return;
        if (rootListView.SelectedItem is null)
        {
            var index = ViewModel.UserInfoManager.ActivatedUserInfo!.SharedWithMeItems.IndexOf(ViewModel.BreadcrumbItems[0]);
            index = Math.Max(index, 0);
            rootListView.SelectedIndex = index;
            ViewModel.RootIndex = index;
        }
        else
        {
            ViewModel.SwitchRootCommand.Execute(rootListView.SelectedItem);
        }
    }
}