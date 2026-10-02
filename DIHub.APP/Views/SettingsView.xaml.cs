using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DIHub.Core;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;

namespace DIHub.APP.Views
{
    public sealed partial class SettingsView : UserControl
    {
        private static readonly (AccentColor Accent, string Hex)[] AccentChoices =
        {
            (AccentColor.Purple, "#8B5CF6"),
            (AccentColor.Blue,   "#3B82F6"),
            (AccentColor.Cyan,   "#06B6D4"),
            (AccentColor.Green,  "#22C55E"),
            (AccentColor.Orange, "#F97316"),
        };

        private static readonly (string Shortcut, string Action)[] Shortcuts =
        {
            ("Ctrl + K",         "Open Command Palette"),
            ("Ctrl + L",         "Focus Address Bar"),
            ("Ctrl + T",         "New Blank Tab"),
            ("Ctrl + W",         "Close Current Tab"),
            ("Ctrl + Shift + T", "Restore Closed Tab"),
            ("Ctrl + R",         "Reload Page (Main) / Reset Layout (Multi-AI)"),
            ("Ctrl + Tab",       "Next Tab / Next Panel"),
            ("Ctrl + Shift + Tab","Previous Tab / Previous Panel"),
            ("Ctrl + Shift + D", "Open Developer Tools"),
            ("Ctrl + Shift + M", "Open Multi-AI Workspace"),
            ("Ctrl + Shift + B", "Toggle Sidebar"),
            ("Ctrl + 1 ... 4",   "Activate Panel N (Multi-AI only)"),
            ("Ctrl + M",         "Maximize Panel (Multi-AI only)"),
            ("Ctrl + ,",         "Open Settings"),
            ("Esc",              "Close Multi-AI / Cancel Broadcast / Close Dialog"),
        };

        private readonly ISettingsService _settings;
        private readonly IThemeService _theme;
        private readonly INotificationService _notifications;
        private readonly IBrowserService _browser;

        private bool _loading;

        public event EventHandler? RequestClose;
        public event EventHandler? RequestClearAllData;

        /// <summary>Raised when the user wants to jump into the Extensions manager.</summary>
        public event EventHandler? RequestOpenExtensions;

        /// <summary>Raised when the user wants to jump into the Diagnostics tab.</summary>
        public event EventHandler? RequestOpenExtensionDiagnostics;

        public SettingsView()
        {
            InitializeComponent();

            _settings = App.GetService<ISettingsService>();
            _theme = App.GetService<IThemeService>();
            _notifications = App.GetService<INotificationService>();
            _browser = App.GetService<IBrowserService>();

            BuildAccentPicker();
            BuildShortcutsList();
        }

        public void Open()
        {
            LoadFromSettings();
            NavList.SelectedIndex = 0;
            ShowSection("general");

            // Version info from central AppInfo (reads assembly version).
            AboutVersionText.Text = AppInfo.Version;
            AboutCopyrightText.Text = AppInfo.Copyright;

            Visibility = Visibility.Visible;
        }

        public void Close()
        {
            Visibility = Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────
        //  Load
        //  ─────────────────────────────────────────────

        private void LoadFromSettings()
        {
            _loading = true;
            var s = _settings.Current;

            // General
            RestoreSessionToggle.IsOn = s.RestorePreviousSession;
            StartWithWindowsToggle.IsOn = s.StartWithWindows;
            ConfirmCloseToggle.IsOn = s.ConfirmOnCloseMultipleTabs;
            RunBackgroundToggle.IsOn = s.RunInBackground;

            // Appearance
            ThemeDark.IsChecked = s.Theme == AppTheme.Dark;
            ThemeLight.IsChecked = s.Theme == AppTheme.Light;
            ThemeSystem.IsChecked = s.Theme == AppTheme.System;
            SidebarDefaultToggle.IsOn = s.SidebarExpandedByDefault;
            AnimationsToggle.IsOn = s.AnimationsEnabled;

            // Browser
            SelectComboByTag(OpenLinksCombo, s.OpenLinks.ToString());
            SelectComboByTag(ExternalBrowserCombo, s.ExternalBrowser.ToString());
            SelectComboByTag(SearchEngineCombo, s.SearchEngine.ToString());

            // Multi-AI
            SelectComboByTag(MultiAIPanelCountCombo, s.MultiAIDefaultPanelCount.ToString());
            MultiAIConfirmSendToggle.IsOn = s.MultiAIConfirmMultiSend;
            MultiAIKeepTargetsToggle.IsOn = s.MultiAIKeepTargetsForNext;
            MultiAIRememberTargetsToggle.IsOn = s.MultiAIRememberLastTargets;
            MultiAIAutoFocusToggle.IsOn = s.MultiAIAutoFocusActivePanel;
            MultiAIAutomaticDispatchToggle.IsOn = s.MultiAIAutomaticDispatch;
            MultiAIProviderAutomationToggle.IsOn = s.MultiAIAllowProviderAutomation;

            // Extensions
            ExtensionsEnabledToggle.IsOn = s.ExtensionsEnabled;
            AllowLocalExtensionsToggle.IsOn = s.AllowLocalExtensions;
            AllowRemoteExtensionsToggle.IsOn = s.AllowRemoteExtensions;
            ExtensionConfirmInstallToggle.IsOn = s.ExtensionConfirmInstall;
            ExtensionShowPermissionWarningsToggle.IsOn = s.ExtensionShowPermissionWarnings;
            ExtensionSafeModeToggle.IsOn = s.ExtensionSafeMode;

            RefreshAccentSelection();
            _loading = false;
        }

        private static void SelectComboByTag(ComboBox combo, string tag)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item && (item.Tag as string) == tag)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────
        //  Navigation
        //  ─────────────────────────────────────────────

        private void OnNavSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedItem is ListViewItem item && item.Tag is string tag)
                ShowSection(tag);
        }

        private void ShowSection(string tag)
        {
            SectionGeneral.Visibility = tag == "general" ? Visibility.Visible : Visibility.Collapsed;
            SectionAppearance.Visibility = tag == "appearance" ? Visibility.Visible : Visibility.Collapsed;
            SectionBrowser.Visibility = tag == "browser" ? Visibility.Visible : Visibility.Collapsed;
            SectionMultiAI.Visibility = tag == "multi-ai" ? Visibility.Visible : Visibility.Collapsed;
            SectionExtensions.Visibility = tag == "extensions" ? Visibility.Visible : Visibility.Collapsed;
            SectionBackup.Visibility = tag == "backup" ? Visibility.Visible : Visibility.Collapsed;
            SectionShortcuts.Visibility = tag == "shortcuts" ? Visibility.Visible : Visibility.Collapsed;
            SectionPrivacy.Visibility = tag == "privacy" ? Visibility.Visible : Visibility.Collapsed;
            SectionReset.Visibility = tag == "reset" ? Visibility.Visible : Visibility.Collapsed;
            SectionAbout.Visibility = tag == "about" ? Visibility.Visible : Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────
        //  Shortcuts list
        //  ─────────────────────────────────────────────

        private void BuildShortcutsList()
        {
            ShortcutsList.Children.Clear();

            for (int i = 0; i < Shortcuts.Length; i++)
            {
                var (shortcut, action) = Shortcuts[i];
                var isLast = i == Shortcuts.Length - 1;

                var row = new Grid { Padding = new Thickness(16, 10, 16, 10) };

                if (!isLast)
                {
                    row.BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];
                    row.BorderThickness = new Thickness(0, 0, 0, 1);
                }

                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var shortcutBorder = new Border
                {
                    Background = (Brush)Application.Current.Resources["AppSurfaceBrush"],
                    BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 3, 8, 3),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                shortcutBorder.Child = new TextBlock
                {
                    Text = shortcut,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["AppTextPrimaryBrush"]
                };

                Grid.SetColumn(shortcutBorder, 0);
                row.Children.Add(shortcutBorder);

                var actionText = new TextBlock
                {
                    Text = action,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.Resources["AppTextPrimaryBrush"]
                };
                Grid.SetColumn(actionText, 1);
                row.Children.Add(actionText);

                ShortcutsList.Children.Add(row);
            }
        }

        // ─────────────────────────────────────────────
        //  General handlers
        //  ─────────────────────────────────────────────

        private void OnRestoreSessionToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.RestorePreviousSession = RestoreSessionToggle.IsOn);
        }

        private void OnStartWithWindowsToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.StartWithWindows = StartWithWindowsToggle.IsOn);
        }

        private void OnConfirmCloseToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.ConfirmOnCloseMultipleTabs = ConfirmCloseToggle.IsOn);
        }

        private void OnRunBackgroundToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.RunInBackground = RunBackgroundToggle.IsOn);
        }

        // ─────────────────────────────────────────────
        //  Appearance
        //  ─────────────────────────────────────────────

        private void OnThemeChanged(object sender, RoutedEventArgs e)
        {
            if (_loading) return;

            var theme = ThemeDark.IsChecked == true ? AppTheme.Dark
                      : ThemeLight.IsChecked == true ? AppTheme.Light
                      : AppTheme.System;

            _theme.SetTheme(theme);
        }

        private void BuildAccentPicker()
        {
            AccentPanel.Children.Clear();

            foreach (var (accent, hex) in AccentChoices)
            {
                var swatch = new Border
                {
                    Width = 28,
                    Height = 28,
                    CornerRadius = new CornerRadius(14),
                    Background = new SolidColorBrush(ParseHex(hex)),
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Colors.Transparent)
                };

                var btn = new Button
                {
                    Content = swatch,
                    Width = 40,
                    Height = 40,
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Tag = accent
                };

                ToolTipService.SetToolTip(btn, accent.ToString());

                btn.Click += (s, e) =>
                {
                    _theme.SetAccent(accent);
                    RefreshAccentSelection();
                };

                AccentPanel.Children.Add(btn);
            }
        }

        private void RefreshAccentSelection()
        {
            var white = new SolidColorBrush(Colors.White);
            var current = _settings.Current.Accent;

            foreach (var child in AccentPanel.Children)
            {
                if (child is not Button btn) continue;
                if (btn.Content is not Border swatch) continue;

                var isSelected = btn.Tag is AccentColor a && a == current;
                swatch.BorderBrush = isSelected ? white : new SolidColorBrush(Colors.Transparent);
            }
        }

        private void OnSidebarDefaultToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.SidebarExpandedByDefault = SidebarDefaultToggle.IsOn);
        }

        private void OnAnimationsToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.AnimationsEnabled = AnimationsToggle.IsOn);
        }

        // ─────────────────────────────────────────────
        //  Browser
        //  ─────────────────────────────────────────────

        private void OnOpenLinksChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (OpenLinksCombo.SelectedItem is not ComboBoxItem item) return;
            if (item.Tag is not string tag) return;
            if (Enum.TryParse<OpenLinksBehavior>(tag, out var val))
                _settings.Update(s => s.OpenLinks = val);
        }

        private void OnExternalBrowserChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (ExternalBrowserCombo.SelectedItem is not ComboBoxItem item) return;
            if (item.Tag is not string tag) return;
            if (Enum.TryParse<ExternalBrowser>(tag, out var val))
                _settings.Update(s => s.ExternalBrowser = val);
        }

        private void OnSearchEngineChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (SearchEngineCombo.SelectedItem is not ComboBoxItem item) return;
            if (item.Tag is not string tag) return;
            if (Enum.TryParse<SearchEngine>(tag, out var val))
                _settings.Update(s => s.SearchEngine = val);
        }

        // ─────────────────────────────────────────────
        //  Multi-AI
        //  ─────────────────────────────────────────────

        private void OnMultiAIPanelCountChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (MultiAIPanelCountCombo.SelectedItem is not ComboBoxItem item) return;
            if (item.Tag is not string tag) return;
            if (!int.TryParse(tag, out var count)) return;

            _settings.Update(s => s.MultiAIDefaultPanelCount = count);
        }

        private void OnMultiAIConfirmSendToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.MultiAIConfirmMultiSend = MultiAIConfirmSendToggle.IsOn);
        }

        private void OnMultiAIKeepTargetsToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.MultiAIKeepTargetsForNext = MultiAIKeepTargetsToggle.IsOn);
        }

        private void OnMultiAIRememberTargetsToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.MultiAIRememberLastTargets = MultiAIRememberTargetsToggle.IsOn);
        }

        private void OnMultiAIAutoFocusToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.MultiAIAutoFocusActivePanel = MultiAIAutoFocusToggle.IsOn);
        }

        private void OnMultiAIAutomaticDispatchToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.MultiAIAutomaticDispatch = MultiAIAutomaticDispatchToggle.IsOn);
        }

        private void OnMultiAIProviderAutomationToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.MultiAIAllowProviderAutomation = MultiAIProviderAutomationToggle.IsOn);
        }

        // ─────────────────────────────────────────────
        //  Extensions handlers
        //  ─────────────────────────────────────────────

        private void OnExtensionsEnabledToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.ExtensionsEnabled = ExtensionsEnabledToggle.IsOn);
        }

        private void OnAllowLocalExtensionsToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.AllowLocalExtensions = AllowLocalExtensionsToggle.IsOn);
        }

        private void OnAllowRemoteExtensionsToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.AllowRemoteExtensions = AllowRemoteExtensionsToggle.IsOn);
        }

        private void OnExtensionConfirmInstallToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.ExtensionConfirmInstall = ExtensionConfirmInstallToggle.IsOn);
        }

        private void OnExtensionShowPermissionWarningsToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.ExtensionShowPermissionWarnings = ExtensionShowPermissionWarningsToggle.IsOn);
        }

        private void OnExtensionSafeModeToggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Update(s => s.ExtensionSafeMode = ExtensionSafeModeToggle.IsOn);
        }

        private void OnOpenExtensionsClicked(object sender, RoutedEventArgs e)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
            RequestOpenExtensions?.Invoke(this, EventArgs.Empty);
        }

        private void OnOpenExtensionDiagnosticsClicked(object sender, RoutedEventArgs e)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
            RequestOpenExtensionDiagnostics?.Invoke(this, EventArgs.Empty);
        }

        // ─────────────────────────────────────────────
        //  Backup / Restore
        //  ─────────────────────────────────────────────

        private async void OnCreateBackupClicked(object sender, RoutedEventArgs e)
        {
            var includeServices = BackupServicesCheck.IsChecked == true;
            var includeExtensions = BackupExtensionsCheck.IsChecked == true;
            var includeSettings = BackupSettingsCheck.IsChecked == true;

            if (!includeServices && !includeExtensions && !includeSettings)
            {
                _notifications.Show("Nothing selected",
                    "Tick at least one item to include in the backup.",
                    NotificationSeverity.Information);
                return;
            }

            var hwnd = GetWindowHandle();
            if (hwnd == IntPtr.Zero)
            {
                _notifications.Show("Backup Error",
                    "Cannot open the save dialog.",
                    NotificationSeverity.Error);
                return;
            }

            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = $"DIHub-Backup-{DateTime.Now:yyyyMMdd-HHmmss}";
            picker.FileTypeChoices.Add("DI Hub Backup", new List<string> { ".dihub" });
            picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file;
            try { file = await picker.PickSaveFileAsync(); }
            catch { return; }

            if (file is null) return;

            try
            {
                var backup = App.GetService<IBackupService>();
                await backup.ExportAsync(
                    file.Path,
                    includeServices,
                    includeExtensions,
                    includeSettings);

                _notifications.Show("Backup Created",
                    $"Saved to {file.Name}",
                    NotificationSeverity.Success);

                LastBackupInfoText.Text =
                    $"Last backup: {DateTime.Now:yyyy-MM-dd HH:mm} → {file.Name}";
                LastBackupInfoText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                _notifications.Show("Backup Failed",
                    ex.Message,
                    NotificationSeverity.Error);
            }
        }

        private async void OnRestoreBackupClicked(object sender, RoutedEventArgs e)
        {
            var hwnd = GetWindowHandle();
            if (hwnd == IntPtr.Zero)
            {
                _notifications.Show("Restore Error",
                    "Cannot open the file dialog.",
                    NotificationSeverity.Error);
                return;
            }

            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".dihub");
            picker.FileTypeFilter.Add(".json");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file;
            try { file = await picker.PickSingleFileAsync(); }
            catch { return; }

            if (file is null) return;

            var backup = App.GetService<IBackupService>();
            var parsed = await backup.ReadAsync(file.Path);

            if (parsed is null)
            {
                _notifications.Show("Invalid Backup",
                    "The selected file is not a valid DI Hub backup.",
                    NotificationSeverity.Error);
                return;
            }

            var sections = parsed.IncludedSections.Length > 0
                ? string.Join(", ", parsed.IncludedSections)
                : "Unknown";

            var confirm = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Restore from backup?",
                Content = new TextBlock
                {
                    Text =
                        $"Backup created: {parsed.CreatedAt:yyyy-MM-dd HH:mm}\n" +
                        $"App version: {parsed.AppVersion}\n" +
                        $"Contains: {sections}\n\n" +
                        "Restoring will overwrite the matching parts of your current configuration.\n\n" +
                        "DI Hub will restart after restore.",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12
                },
                PrimaryButtonText = "Restore",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirm.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                await backup.ApplyAsync(parsed);

                _notifications.Show("Restore Complete",
                    "Configuration restored. Restarting DI Hub...",
                    NotificationSeverity.Success);

                await Task.Delay(1200);

                try
                {
                    var exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                        System.Diagnostics.Process.Start(exePath);
                }
                catch { }

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _notifications.Show("Restore Failed",
                    ex.Message,
                    NotificationSeverity.Error);
            }
        }

        // ─────────────────────────────────────────────
        //  Reset
        //  ─────────────────────────────────────────────

        private async void OnResetSelectedClicked(object sender, RoutedEventArgs e)
        {
            var selected = new List<string>();
            if (ResetAiServicesCheck.IsChecked == true) selected.Add("AI services");
            if (ResetExtensionsCheck.IsChecked == true) selected.Add("extensions");
            if (ResetPromptHistoryCheck.IsChecked == true) selected.Add("prompt history");
            if (ResetSettingsCheck.IsChecked == true) selected.Add("settings");
            if (ResetWindowCheck.IsChecked == true) selected.Add("window position");

            if (selected.Count == 0)
            {
                _notifications.Show("Nothing selected",
                    "Tick at least one item to reset.",
                    NotificationSeverity.Information);
                return;
            }

            var confirm = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Reset selected items?",
                Content = new TextBlock
                {
                    Text = "The following will be reset to defaults:\n\n  • " +
                           string.Join("\n  • ", selected) +
                           "\n\nThis cannot be undone.",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12
                },
                PrimaryButtonText = "Reset",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirm.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            int done = 0;

            if (ResetAiServicesCheck.IsChecked == true)
            {
                try
                {
                    var svcMgr = App.GetService<IAIServiceManager>();
                    svcMgr.ResetToDefaults();
                    done++;
                }
                catch { }
            }

            if (ResetExtensionsCheck.IsChecked == true)
            {
                try
                {
                    var extMgr = App.GetService<IExtensionManager>();
                    foreach (var ext in extMgr.InstalledExtensions.ToList())
                        await extMgr.RemoveAsync(ext.Id);
                    done++;
                }
                catch { }
            }

            if (ResetPromptHistoryCheck.IsChecked == true)
            {
                try
                {
                    var presets = App.GetService<IPresetManager>();
                    presets.ClearHistory();
                    done++;
                }
                catch { }
            }

            if (ResetSettingsCheck.IsChecked == true)
            {
                try
                {
                    _settings.Update(s =>
                    {
                        s.RestorePreviousSession = true;
                        s.StartWithWindows = false;
                        s.ConfirmOnCloseMultipleTabs = true;
                        s.RunInBackground = false;
                        s.Theme = AppTheme.Dark;
                        s.Accent = AccentColor.Purple;
                        s.SidebarExpandedByDefault = true;
                        s.AnimationsEnabled = true;
                        s.OpenLinks = OpenLinksBehavior.NewTab;
                        s.ExternalBrowser = ExternalBrowser.Default;
                        s.SearchEngine = SearchEngine.Google;
                        s.MultiAIDefaultPanelCount = 2;
                        s.MultiAIConfirmMultiSend = true;
                        s.MultiAIKeepTargetsForNext = false;
                        s.MultiAIRememberLastTargets = true;
                        s.MultiAIAutoFocusActivePanel = true;
                        s.MultiAIAutomaticDispatch = true;
                        s.MultiAIAllowProviderAutomation = true;
                        s.ExtensionsEnabled = true;
                        s.AllowLocalExtensions = true;
                        s.AllowRemoteExtensions = true;
                        s.ExtensionConfirmInstall = true;
                        s.ExtensionShowPermissionWarnings = true;
                        s.ExtensionSafeMode = false;
                    });
                    await _settings.SaveAsync();
                    LoadFromSettings();
                    done++;
                }
                catch { }
            }

            if (ResetWindowCheck.IsChecked == true)
            {
                try
                {
                    var win = App.GetMainWindow();
                    if (win is not null)
                    {
                        win.AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
                        win.AppWindow.Move(new Windows.Graphics.PointInt32(100, 100));
                    }
                    done++;
                }
                catch { }
            }

            _notifications.Show("Reset complete",
                $"{done} item(s) restored to defaults.",
                NotificationSeverity.Success);
        }

        private async void OnResetEverythingClicked(object sender, RoutedEventArgs e)
        {
            var confirm = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "⚠ Reset everything?",
                Content = new TextBlock
                {
                    Text = "This will reset:\n\n" +
                           "  • All AI services (back to 13 factory defaults)\n" +
                           "  • All extensions (uninstalled)\n" +
                           "  • All prompt history\n" +
                           "  • All settings (theme, accent, browser, ...)\n" +
                           "  • Window position and size\n\n" +
                           "DI Hub will restart automatically.\n\n" +
                           "⚠ Your browser profiles and login sessions are NOT deleted — " +
                           "you will stay signed in to your AI accounts.",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12
                },
                PrimaryButtonText = "Reset and Restart",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirm.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try { App.GetService<IAIServiceManager>().ResetToDefaults(); } catch { }
            try { App.GetService<IPresetManager>().ClearHistory(); } catch { }

            try
            {
                var extMgr = App.GetService<IExtensionManager>();
                foreach (var ext in extMgr.InstalledExtensions.ToList())
                    await extMgr.RemoveAsync(ext.Id);
            }
            catch { }

            try
            {
                _settings.Update(s =>
                {
                    s.RestorePreviousSession = true;
                    s.StartWithWindows = false;
                    s.ConfirmOnCloseMultipleTabs = true;
                    s.RunInBackground = false;
                    s.Theme = AppTheme.Dark;
                    s.Accent = AccentColor.Purple;
                    s.SidebarExpandedByDefault = true;
                    s.AnimationsEnabled = true;
                    s.OpenLinks = OpenLinksBehavior.NewTab;
                    s.ExternalBrowser = ExternalBrowser.Default;
                    s.SearchEngine = SearchEngine.Google;
                    s.MultiAIDefaultPanelCount = 2;
                    s.MultiAIConfirmMultiSend = true;
                    s.MultiAIKeepTargetsForNext = false;
                    s.MultiAIRememberLastTargets = true;
                    s.MultiAIAutoFocusActivePanel = true;
                    s.MultiAIAutomaticDispatch = true;
                    s.MultiAIAllowProviderAutomation = true;
                    s.ExtensionsEnabled = true;
                    s.AllowLocalExtensions = true;
                    s.AllowRemoteExtensions = true;
                    s.ExtensionConfirmInstall = true;
                    s.ExtensionShowPermissionWarnings = true;
                    s.ExtensionSafeMode = false;
                    s.WindowState = new WindowStateModel
                    {
                        X = 100,
                        Y = 100,
                        Width = 1400,
                        Height = 900,
                        IsMaximized = false
                    };
                });
                await _settings.SaveAsync();
            }
            catch { }

            try
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                    System.Diagnostics.Process.Start(exePath);
            }
            catch { }

            Environment.Exit(0);
        }

        // ─────────────────────────────────────────────
        //  About / Links
        //  ─────────────────────────────────────────────

        private void OnOpenLinkClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url)
                _browser.OpenInDefaultBrowser(url);
        }

        // ─────────────────────────────────────────────
        //  Privacy
        //  ─────────────────────────────────────────────

        private async void OnClearAllDataClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Clear all browsing data?",
                Content = "This will sign you out from all AI services and remove all cached data. " +
                          "This action cannot be undone.",
                PrimaryButtonText = "Clear All",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            RequestClearAllData?.Invoke(this, EventArgs.Empty);
            _notifications.Show("Data Cleared", "All browsing data has been removed.");
        }

        // ─────────────────────────────────────────────
        //  Close
        //  ─────────────────────────────────────────────

        private void OnCloseClicked(object sender, RoutedEventArgs e)
            => RequestClose?.Invoke(this, EventArgs.Empty);

        private void OnBackdropTapped(object sender, TappedRoutedEventArgs e)
            => RequestClose?.Invoke(this, EventArgs.Empty);

        private void OnPanelTapped(object sender, TappedRoutedEventArgs e)
            => e.Handled = true;

        private static IntPtr GetWindowHandle()
        {
            try
            {
                var win = App.GetMainWindow();
                if (win is null) return IntPtr.Zero;
                return WinRT.Interop.WindowNative.GetWindowHandle(win);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static Color ParseHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
    }
}