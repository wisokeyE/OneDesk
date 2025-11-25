using System.Windows.Controls;
using OneDesk.Models;
using OneDesk.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using ListView = Wpf.Ui.Controls.ListView;

namespace OneDesk.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            AppConfig appConfig
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            if (appConfig.FollowSystemTheme)
            {
                SystemThemeWatcher.Watch(this);
            }

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        public void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (sender is not ListView userListView) return;
            var userInfoManager = ViewModel.UserInfoManager;
            if (userInfoManager.IsLocked)
            {
                userListView.SelectedItem = userInfoManager.ActivatedUserInfo;
                return;
            }
            if (userListView.SelectedItem is null)
            {
                if (userInfoManager.ActivatedUserInfo is not null)
                {
                    userListView.SelectedItem = userInfoManager.ActivatedUserInfo;
                }
            }
            else
            {
                userInfoManager.ActiveClient(userListView.SelectedIndex);
            }
        }
    }
}
