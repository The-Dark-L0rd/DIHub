using System;
using DIHub.Core.Interfaces;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
            Visibility = Visibility.Visible;
        }

        public void Close()
        {
            Visibility = Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────
        //  Load
        // ─────────────────────────────────────────────

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
        // ─────────────────────────────────────────────

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
            SectionShortcuts.Visibility = tag == "shortcuts" ? Visibility.Visible : Visibility.Collapsed;
            SectionPrivacy.Visibility = tag == "privacy" ? Visibility.Visible : Visibility.Collapsed;
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
        // ─────────────────────────────────────────────

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