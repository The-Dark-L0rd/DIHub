using System;
using System.Linq;
using System.Threading.Tasks;
using DIHub.Core;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Windows.UI;

namespace DIHub.APP.Controls.Extensions
{
    public sealed partial class ExtensionDiagnosticsPanel : UserControl
    {
        private readonly IExtensionManager _manager;
        private readonly ISettingsService _settings;
        private readonly IAIAccountManager _accounts;
        private readonly IAIServiceManager _services;

        public ExtensionDiagnosticsPanel()
        {
            InitializeComponent();
            _manager = App.GetService<IExtensionManager>();
            _settings = App.GetService<ISettingsService>();
            _accounts = App.GetService<IAIAccountManager>();
            _services = App.GetService<IAIServiceManager>();

            Loaded += (_, __) => Refresh();
        }

        private void OnRefreshClicked(object sender, RoutedEventArgs e) => Refresh();

        // ─────────────────────────────────────────────
        //  Refresh
        // ─────────────────────────────────────────────

        public void Refresh()
        {
            try
            {
                BuildEnvironmentPanel();
                BuildExtensionsList();
                BuildAssignmentsList();
            }
            catch { }
        }

        private void BuildEnvironmentPanel()
        {
            EnvironmentPanel.Children.Clear();

            // WebView2 runtime version
            string runtimeVersion = "(unavailable)";
            try
            {
                runtimeVersion = CoreWebView2Environment.GetAvailableBrowserVersionString()
                                 ?? runtimeVersion;
            }
            catch { }

            AddRow(EnvironmentPanel, "WebView2 Runtime", runtimeVersion);
            AddRow(EnvironmentPanel, "DI Hub Version", AppInfo.Version);

            // Master switch (as stored in settings)
            AddRow(EnvironmentPanel,
                "Browser extensions (setting)",
                _settings.Current.ExtensionsEnabled ? "Enabled" : "Disabled");

            // Note about runtime
            AddRow(EnvironmentPanel,
                "Runtime extension subsystem",
                _settings.Current.ExtensionsEnabled
                    ? "Enabled (requires restart if changed)"
                    : "Disabled — extensions will not load");

            AddRow(EnvironmentPanel,
                "Safe mode",
                _settings.Current.ExtensionSafeMode ? "On" : "Off");

            AddRow(EnvironmentPanel,
                "Allow local installs",
                _settings.Current.AllowLocalExtensions ? "Yes" : "No");

            AddRow(EnvironmentPanel,
                "Allow remote installs",
                _settings.Current.AllowRemoteExtensions ? "Yes" : "No");
        }

        private static void AddRow(Panel panel, string label, string value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["AppTextSecondaryBrush"]
            };
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = value,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["AppTextPrimaryBrush"]
            };
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);

            panel.Children.Add(grid);
        }

        private void BuildExtensionsList()
        {
            ExtensionsList.Children.Clear();

            var installed = _manager.InstalledExtensions;
            if (installed.Count == 0)
            {
                NoExtensionsText.Visibility = Visibility.Visible;
                return;
            }

            NoExtensionsText.Visibility = Visibility.Collapsed;

            foreach (var ext in installed.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            {
                ExtensionsList.Children.Add(BuildExtensionRow(ext));
            }
        }

        private Border BuildExtensionRow(ExtensionInfo ext)
        {
            var stack = new StackPanel { Spacing = 3 };

            stack.Children.Add(new TextBlock
            {
                Text = $"{ext.Name}  ·  v{ext.Version}",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["AppTextPrimaryBrush"]
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Source: {ext.Source}   |   Manifest v{ext.ManifestVersion}",
                FontSize = 10,
                Opacity = 0.7,
                Foreground = (Brush)Application.Current.Resources["AppTextSecondaryBrush"]
            });

            stack.Children.Add(new TextBlock
            {
                Text = ext.InstallPath,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                Foreground = (Brush)Application.Current.Resources["AppTextSecondaryBrush"]
            });

            if (ext.Permissions.Length > 0 || ext.HostPermissions.Length > 0)
            {
                var total = ext.Permissions.Length + ext.HostPermissions.Length;
                stack.Children.Add(new TextBlock
                {
                    Text = $"{total} permission(s) declared",
                    FontSize = 10,
                    Opacity = 0.6,
                    Foreground = (Brush)Application.Current.Resources["AppTextSecondaryBrush"]
                });
            }

            return new Border
            {
                Background = (Brush)Application.Current.Resources["AppSurfaceBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Child = stack
            };
        }

        private void BuildAssignmentsList()
        {
            AssignmentsList.Children.Clear();

            var assignments = _manager.Assignments;
            if (assignments.Count == 0)
            {
                NoAssignmentsText.Visibility = Visibility.Visible;
                return;
            }

            NoAssignmentsText.Visibility = Visibility.Collapsed;

            foreach (var a in assignments.OrderBy(a => a.Scope))
            {
                AssignmentsList.Children.Add(BuildAssignmentRow(a));
            }
        }

        private Border BuildAssignmentRow(ExtensionAssignment a)
        {
            var ext = _manager.GetExtension(a.ExtensionId);
            var extName = ext?.Name ?? a.ExtensionId;

            var scopeLabel = a.Scope switch
            {
                ExtensionScope.Global => "Global",
                ExtensionScope.Service => "Service",
                ExtensionScope.Account => "Account",
                _ => "?"
            };

            // Resolve friendly target labels.
            string target;
            switch (a.Scope)
            {
                case ExtensionScope.Global:
                    target = "—";
                    break;

                case ExtensionScope.Service:
                    target = _services.FindService(a.ServiceId ?? string.Empty)?.Name ?? "(unknown service)";
                    break;

                case ExtensionScope.Account:
                    var acc = _accounts.GetAccount(a.AccountId ?? string.Empty);
                    var svc = acc is not null ? _services.FindService(acc.ServiceId) : null;
                    target = acc is null
                        ? "(unknown account)"
                        : $"{svc?.Name ?? "?"} / {acc.Name}";
                    break;

                default:
                    target = "—";
                    break;
            }

            var statusText = a.IsEnabled ? "ENABLED" : "DISABLED";
            var statusColor = a.IsEnabled
                ? Color.FromArgb(255, 0x22, 0xC5, 0x5E)
                : Color.FromArgb(255, 0x9C, 0xA3, 0xAF);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { Spacing = 2 };
            left.Children.Add(new TextBlock
            {
                Text = extName,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["AppTextPrimaryBrush"]
            });
            left.Children.Add(new TextBlock
            {
                Text = $"{scopeLabel} → {target}",
                FontSize = 10,
                Foreground = (Brush)Application.Current.Resources["AppTextSecondaryBrush"]
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var status = new TextBlock
            {
                Text = statusText,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(statusColor)
            };
            Grid.SetColumn(status, 1);
            grid.Children.Add(status);

            return new Border
            {
                Background = (Brush)Application.Current.Resources["AppSurfaceBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Child = grid
            };
        }
    }
}