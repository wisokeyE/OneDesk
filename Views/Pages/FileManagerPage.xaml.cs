using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Graph.Models;
using OneDesk.Models;
using OneDesk.ViewModels.Pages;
using OneDesk.Views.Windows;
using Wpf.Ui.Abstractions.Controls;
using ListView = Wpf.Ui.Controls.ListView;
using ListViewItem = Wpf.Ui.Controls.ListViewItem;

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

    private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListViewItem listViewItem) return;
        if (listViewItem.Content is not DriveItem item) return;

        // 如果是文件夹，进入文件夹
        if (item.Folder is not null)
        {
            var currentFolder = ViewModel.CurrentFolder;
            var newPath = Path.Combine(currentFolder.Path, item.Name!).Replace("\\", "/");
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

    private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var tempList = FileListView.SelectedItems.Cast<DriveItem>().ToList();

        // 同步选中项到 ViewModel
        ViewModel.SelectedItems = [.. tempList];
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

    private void FileListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // 获取鼠标位置
        var mousePosition = Mouse.GetPosition(FileListView);

        // 检查鼠标位置是否在列头上
        var hitTestResult = VisualTreeHelper.HitTest(FileListView, mousePosition);
        if (hitTestResult?.VisualHit == null) return;

        // 查找是否点击在 GridViewColumnHeader 上
        var header = FindVisualParent<GridViewColumnHeader>(hitTestResult.VisualHit);
        if (header == null) return;

        // 如果点击在列头上，取消菜单打开
        e.Handled = true;
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        return parentObject switch
        {
            null => null,
            T parent => parent,
            _ => FindVisualParent<T>(parentObject)
        };
    }
}