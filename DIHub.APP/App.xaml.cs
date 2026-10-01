using CommunityToolkit.Mvvm.Messaging;
using DIHub.APP.Services;
using DIHub.APP.ViewModels;
using DIHub.Core.Interfaces;
using DIHub.Core.Services;
using DIHub.Infrastructure.Storage;
using DIHub.Infrastructure.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Threading.Tasks;

namespace DIHub.APP
{
    public partial class App : Application
    {
        private const string SingleInstanceKey = "DIHub_MainInstance_v1";

        public IHost Host { get; }

        public static T GetService<T>() where T : class
            => ((App)Current).Host.Services.GetRequiredService<T>();

        private Window? _mainWindow;

        public App()
        {
            InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

                    services.AddSingleton<IConfigurationStorage, JsonConfigurationStorage>();
                    services.AddSingleton<IBrowserService, BrowserService>();

                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<INotificationService, NotificationService>();
                    services.AddSingleton<IAIServiceManager, AIServiceManager>();
                    services.AddSingleton<IWorkspaceManager, WorkspaceManager>();
                    services.AddSingleton<IAIAccountManager, AIAccountManager>();
                    services.AddSingleton<IMultiAIWorkspaceManager, MultiAIWorkspaceManager>();
                    services.AddSingleton<IPresetManager, PresetManager>();
                    services.AddSingleton<PromptDispatcher>();

                    services.AddSingleton<MainWindowViewModel>();
                })
                .Build();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Single-instance handling
            try
            {
                var mainInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);

                if (!mainInstance.IsCurrent)
                {
                    var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                    _ = mainInstance.RedirectActivationToAsync(activatedArgs);
                    Environment.Exit(0);
                    return;
                }

                mainInstance.Activated += OnAppInstanceActivated;
            }
            catch { }

            _ = BootstrapAsync();
        }

        private void OnAppInstanceActivated(object? sender, AppActivationArguments e)
        {
            try
            {
                if (_mainWindow is null) return;

                _mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow);
                        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                        {
                            if (presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)
                                presenter.Restore();
                        }

                        _mainWindow.Activate();
                    }
                    catch { }
                });
            }
            catch { }
        }

        private async Task BootstrapAsync()
        {
            try
            {
                var settings = GetService<ISettingsService>();
                await settings.LoadAsync();

                var services = GetService<IAIServiceManager>();
                await services.LoadAsync();

                var workspaces = GetService<IWorkspaceManager>();
                await workspaces.LoadAsync();

                var accounts = GetService<IAIAccountManager>();
                await accounts.LoadAsync();

                var multiAI = GetService<IMultiAIWorkspaceManager>();
                await multiAI.LoadAsync();

                var presets = GetService<IPresetManager>();
                await presets.LoadAsync();
            }
            catch { }

            _mainWindow = new MainWindow();
            _mainWindow.Activate();
        }
    }
}