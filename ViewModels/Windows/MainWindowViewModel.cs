using System.Collections.ObjectModel;
using OneDesk.Services.Auth;
using Wpf.Ui.Controls;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace OneDesk.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "WPF UI - OneDesk";

        [ObservableProperty]
        private IUserInfoManager _userInfoManager;

        [ObservableProperty]
        private bool _popupIsOpen;

        [ObservableProperty]
        private ObservableCollection<object> _menuItems =
        [
            new NavigationViewItem()
            {
                Content = "文件管理",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Cloud24 },
                TargetPageType = typeof(Views.Pages.FileManagerPage)
            },
            new NavigationViewItem()
            {
                Content = "任务管理",
                Icon = new SymbolIcon { Symbol = SymbolRegular.TaskListSquareLtr24 },
                TargetPageType = typeof(Views.Pages.TaskManagerPage)
            }
        ];

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems =
        [
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        ];

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems =
        [
            new() { Header = "Home", Tag = "tray_home" }
        ];

        public MainWindowViewModel(IUserInfoManager userInfoManager)
        {
            _userInfoManager = userInfoManager;
        }

        [RelayCommand]
        private void TogglePopup()
        {
            PopupIsOpen = !UserInfoManager.IsLocked && !PopupIsOpen;
        }

        [RelayCommand]
        private void AddUser()
        {
            UserInfoManager.AddClient();
        }

        [RelayCommand]
        private void RemoveCurrentUser()
        {
            UserInfoManager.RemoveClient();
            if (UserInfoManager.ActivatedUserInfo is null)
            {
                PopupIsOpen = false;
            }
        }
    }
}
