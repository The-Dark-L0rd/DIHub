using System.Threading.Tasks;
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

namespace DIHub.APP
{
    public partial class App : Application
    {
        public IHost Host { get; }

        public static T GetService<T>() where T : class
            => ((App)Current).Host.Services.GetRequiredService<T>();

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
            _ = BootstrapAsync();
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

            var window = new MainWindow();
            window.Activate();
        }
    }
}