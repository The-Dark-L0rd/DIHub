using System;
using DIHub.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DIHub.APP.Controls.Extensions
{
    public sealed partial class InstalledExtensionItem : UserControl
    {
        public event EventHandler<ExtensionActionEventArgs>? ActionRequested;

        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register(
                nameof(Item),
                typeof(ExtensionInfo),
                typeof(InstalledExtensionItem),
                new PropertyMetadata(null, OnItemChanged));

        public static readonly DependencyProperty EffectiveStateProperty =
            DependencyProperty.Register(
                nameof(EffectiveState),
                typeof(ExtensionEffectiveState),
                typeof(InstalledExtensionItem),
                new PropertyMetadata(null, OnEffectiveStateChanged));

        public ExtensionInfo? Item
        {
            get => (ExtensionInfo?)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public ExtensionEffectiveState? EffectiveState
        {
            get => (ExtensionEffectiveState?)GetValue(EffectiveStateProperty);
            set => SetValue(EffectiveStateProperty, value);
        }

        public InstalledExtensionItem()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────
        //  DP callbacks
        // ─────────────────────────────────────────────

        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InstalledExtensionItem item) item.Refresh();
        }

        private static void OnEffectiveStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InstalledExtensionItem item) item.Refresh();
        }

        // ─────────────────────────────────────────────
        //  Render
        // ─────────────────────────────────────────────

        private void Refresh()
        {
            var ext = Item;
            if (ext is null)
            {
                NameText.Text = "(no extension)";
                VersionText.Text = string.Empty;
                StatusText.Text = string.Empty;
                ScopeText.Text = string.Empty;
                return;
            }

            NameText.Text = ext.Name;
            VersionText.Text = string.IsNullOrEmpty(ext.Version)
                ? string.Empty
                : "v" + ext.Version;

            // Icon
            var imageSource = ExtensionIconHelper.TryGetIcon(ext);
            if (imageSource is not null)
            {
                IconImage.Source = imageSource;
                IconImage.Visibility = Visibility.Visible;
                IconGlyph.Visibility = Visibility.Collapsed;
            }
            else
            {
                IconImage.Visibility = Visibility.Collapsed;
                IconGlyph.Visibility = Visibility.Visible;
            }

            // Enabled / Disabled
            var isEnabled = EffectiveState?.IsEnabled == true;
            StatusText.Text = isEnabled ? "Enabled" : "Disabled";
            ToggleText.Text = isEnabled ? "Disable" : "Enable";

            var accentBrush = (Brush?)Application.Current.Resources["AppAccentBrush"]
                              ?? new SolidColorBrush(Colors.Purple);
            var mutedBrush = (Brush?)Application.Current.Resources["AppDisabledBrush"]
                             ?? new SolidColorBrush(Colors.Gray);

            StatusDot.Fill = isEnabled ? accentBrush : mutedBrush;

            // Scope badge
            var scope = EffectiveState?.Source ?? ExtensionScope.None;
            ScopeText.Text = ScopeLabel(scope);
            ScopeBadge.Visibility = Visibility.Visible;

            // Source badge
            var source = SourceLabel(ext.Source);
            if (!string.IsNullOrEmpty(source))
            {
                SourceText.Text = source;
                SourceBadge.Visibility = Visibility.Visible;
            }
            else
            {
                SourceBadge.Visibility = Visibility.Collapsed;
            }
        }

        private static string ScopeLabel(ExtensionScope scope) => scope switch
        {
            ExtensionScope.Account => "Account",
            ExtensionScope.Service => "Service",
            ExtensionScope.Global => "Global",
            _ => "Default"
        };

        private static string SourceLabel(ExtensionSourceType source) => source switch
        {
            ExtensionSourceType.Catalog => "Catalog",
            ExtensionSourceType.Official => "Official",
            ExtensionSourceType.Community => "Community",
            ExtensionSourceType.LocalFolder => "Local folder",
            ExtensionSourceType.LocalZip => "Local ZIP",
            _ => string.Empty
        };

        // ─────────────────────────────────────────────
        //  Actions
        // ─────────────────────────────────────────────

        private void OnToggleClicked(object sender, RoutedEventArgs e)
        {
            var isEnabled = EffectiveState?.IsEnabled == true;
            ActionRequested?.Invoke(this, new ExtensionActionEventArgs
            {
                Kind = isEnabled ? ExtensionActionKind.Disable : ExtensionActionKind.Enable,
                InstalledExtension = Item
            });
        }

        private void OnManageScopeClicked(object sender, RoutedEventArgs e)
        {
            ActionRequested?.Invoke(this, new ExtensionActionEventArgs
            {
                Kind = ExtensionActionKind.ManageScope,
                InstalledExtension = Item
            });
        }

        private void OnDetailsClicked(object sender, RoutedEventArgs e)
        {
            ActionRequested?.Invoke(this, new ExtensionActionEventArgs
            {
                Kind = ExtensionActionKind.Details,
                InstalledExtension = Item
            });
        }

        private void OnRemoveClicked(object sender, RoutedEventArgs e)
        {
            ActionRequested?.Invoke(this, new ExtensionActionEventArgs
            {
                Kind = ExtensionActionKind.Remove,
                InstalledExtension = Item
            });
        }
    }
}