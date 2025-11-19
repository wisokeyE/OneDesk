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
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Home",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "Data",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
                TargetPageType = typeof(Views.Pages.DataPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };

        public MainWindowViewModel(IUserInfoManager userInfoManager)
        {
            _userInfoManager = userInfoManager;
        }

        [RelayCommand]
        private void TogglePopup()
        {
            PopupIsOpen = !PopupIsOpen;
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
