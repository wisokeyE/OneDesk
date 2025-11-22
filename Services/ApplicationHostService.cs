using Microsoft.Extensions.Hosting;
using OneDesk.Services.Configuration;
using OneDesk.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace OneDesk.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService(IServiceProvider serviceProvider, IConfigService configService) : IHostedService
    {
        private INavigationWindow _navigationWindow = null!;

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await configService.LoadAsync();
            configService.WatchConfig();

            HandleThemeInit();
            await HandleActivationAsync();
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await configService.SaveAsync();
        }

        private void HandleThemeInit()
        {
            var config = configService.Config;
            ApplyConfigTheme();

            // 处理跟随系统主题变化
            var oldFollowSystemTheme = config.FollowSystemTheme;
            config.PropertyChanged += (_, _) =>
            {
                if (oldFollowSystemTheme == config.FollowSystemTheme) return;
                oldFollowSystemTheme = config.FollowSystemTheme;
                var window = Application.Current.MainWindow;
                if (oldFollowSystemTheme)
                {
                    SystemThemeWatcher.Watch(window);
                    ApplicationThemeManager.ApplySystemTheme();
                }
                else
                {
                    SystemThemeWatcher.UnWatch(window);
                    ApplyConfigTheme();
                }
            };
            return;

            void ApplyConfigTheme()
            {
                if (Enum.TryParse(config.Theme, true, out ApplicationTheme theme))
                {
                    ApplicationThemeManager.Apply(theme);
                }

                config.Theme = ApplicationThemeManager.GetAppTheme().ToString();
            }
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                _navigationWindow = (
                    serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow
                )!;
                _navigationWindow!.ShowWindow();

                _navigationWindow.Navigate(typeof(Views.Pages.FileManagerPage));
            }

            await Task.CompletedTask;
        }
    }
}
