using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DIHub.APP.Controls.Extensions
{
    public sealed partial class ExtensionPermissionList : UserControl
    {
        public static readonly DependencyProperty PermissionsProperty =
            DependencyProperty.Register(
                nameof(Permissions),
                typeof(IReadOnlyList<string>),
                typeof(ExtensionPermissionList),
                new PropertyMetadata(null, OnPermissionsChanged));

        public static readonly DependencyProperty HostPermissionsProperty =
            DependencyProperty.Register(
                nameof(HostPermissions),
                typeof(IReadOnlyList<string>),
                typeof(ExtensionPermissionList),
                new PropertyMetadata(null, OnPermissionsChanged));

        public IReadOnlyList<string>? Permissions
        {
            get => (IReadOnlyList<string>?)GetValue(PermissionsProperty);
            set => SetValue(PermissionsProperty, value);
        }

        public IReadOnlyList<string>? HostPermissions
        {
            get => (IReadOnlyList<string>?)GetValue(HostPermissionsProperty);
            set => SetValue(HostPermissionsProperty, value);
        }

        public ExtensionPermissionList()
        {
            InitializeComponent();
            Refresh();
        }

        private static void OnPermissionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtensionPermissionList list) list.Refresh();
        }

        private void Refresh()
        {
            RowsPanel.Children.Clear();

            var all = new List<(string Raw, string Friendly, bool IsHost)>();

            if (Permissions is not null)
                foreach (var p in Permissions)
                    if (!string.IsNullOrWhiteSpace(p))
                        all.Add((p, FriendlyLabel(p), false));

            if (HostPermissions is not null)
                foreach (var h in HostPermissions)
                    if (!string.IsNullOrWhiteSpace(h))
                        all.Add((h, FriendlyHostLabel(h), true));

            if (all.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            EmptyText.Visibility = Visibility.Collapsed;

            foreach (var (raw, friendly, isHost) in all)
                RowsPanel.Children.Add(BuildRow(raw, friendly, isHost));
        }

        private Border BuildRow(string raw, string friendly, bool isHost)
        {
            var stack = new StackPanel { Spacing = 2 };

            stack.Children.Add(new TextBlock
            {
                Text = friendly,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["AppTextPrimaryBrush"]
            });

            stack.Children.Add(new TextBlock
            {
                Text = isHost ? "Host: " + raw : raw,
                FontSize = 10,
                Opacity = 0.65,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["AppTextSecondaryBrush"]
            });

            // Broad site access warning.
            if (isHost && (raw.Contains("<all_urls>") || raw.Contains("*://*/*")))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "⚠ Broad site access",
                    FontSize = 10,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 0xF9, 0x73, 0x16))
                });
            }

            return new Border
            {
                Background = (Brush)Application.Current.Resources["AppSecondaryBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Child = stack
            };
        }

        // ─────────────────────────────────────────────
        //  Friendly labels
        // ─────────────────────────────────────────────

        private static string FriendlyLabel(string raw) => raw switch
        {
            "storage" => "Storage — Save extension data",
            "tabs" => "Tabs — Read and interact with open tabs",
            "activeTab" => "Active tab — Access the current tab when you click",
            "webRequest" => "Web request — Observe network requests",
            "webRequestBlocking" => "Web request blocking — Block or modify network requests",
            "contextMenus" => "Context menus — Add items to right-click menus",
            "notifications" => "Notifications — Show system notifications",
            "cookies" => "Cookies — Read and modify cookies",
            "history" => "History — Read and modify browsing history",
            "downloads" => "Downloads — Manage downloads",
            "bookmarks" => "Bookmarks — Read and modify bookmarks",
            "clipboardRead" => "Clipboard read — Read clipboard content",
            "clipboardWrite" => "Clipboard write — Write to clipboard",
            "scripting" => "Scripting — Inject scripts into pages",
            "declarativeNetRequest" => "Network rules — Modify network requests (MV3)",
            "webNavigation" => "Navigation — Observe page navigations",
            "alarms" => "Alarms — Schedule background work",
            "idle" => "Idle detection — Detect idle state",
            "unlimitedStorage" => "Unlimited storage — Use more local storage",
            _ => raw
        };

        private static string FriendlyHostLabel(string raw)
        {
            if (raw.Contains("<all_urls>") || raw.Contains("*://*/*"))
                return "Web access — All websites";

            if (raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return "Web access — Specific HTTPS site(s)";

            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                return "Web access — Specific HTTP site(s)";

            return "Web access";
        }
    }
}