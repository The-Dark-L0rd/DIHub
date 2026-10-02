using DIHub.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Linq;

namespace DIHub.APP.Controls.Extensions
{
    public sealed partial class ExtensionCard : UserControl
    {
        public event EventHandler<ExtensionActionEventArgs>? ActionRequested;

        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register(
                nameof(Item),
                typeof(ExtensionCatalogItem),
                typeof(ExtensionCard),
                new PropertyMetadata(null, OnItemChanged));

        public static readonly DependencyProperty IsInstalledProperty =
            DependencyProperty.Register(
                nameof(IsInstalled),
                typeof(bool),
                typeof(ExtensionCard),
                new PropertyMetadata(false, OnIsInstalledChanged));

        public ExtensionCatalogItem? Item
        {
            get => (ExtensionCatalogItem?)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public bool IsInstalled
        {
            get => (bool)GetValue(IsInstalledProperty);
            set => SetValue(IsInstalledProperty, value);
        }

        public ExtensionCard()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────
        //  DP callbacks
        // ─────────────────────────────────────────────

        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtensionCard card)
                card.Refresh();
        }

        private static void OnIsInstalledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtensionCard card)
                card.RefreshInstalledBadge();
        }

        // ─────────────────────────────────────────────
        //  Render
        // ─────────────────────────────────────────────

        private void Refresh()
        {
            var item = Item;
            if (item is null)
            {
                NameText.Text = "(no item)";
                VersionText.Text = string.Empty;
                DescriptionText.Text = string.Empty;
                CategoryText.Text = string.Empty;
                return;
            }

            NameText.Text = item.Name;
            VersionText.Text = string.IsNullOrEmpty(item.Version) ? string.Empty : "v" + item.Version;
            DescriptionText.Text = item.Description ?? string.Empty;

            var category = item.Categories?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
            CategoryText.Text = string.IsNullOrEmpty(category) ? "General" : category;
            CategoryBadge.Visibility = Visibility.Visible;

            // Icon: try image, else glyph.
            var imageSource = BuildIconFromGlyphOrNull(item);
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

                var glyph = string.IsNullOrEmpty(item.Glyph) ? "\uE7B8" : item.Glyph!;
                IconGlyph.Glyph = glyph;
            }

            RefreshInstalledBadge();
        }

        private void RefreshInstalledBadge()
        {
            if (InstalledBadge is null) return;
            InstalledBadge.Visibility = IsInstalled ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Catalog entries only carry a Glyph (no disk icon yet).
        /// This helper always returns null for now — kept for future use.
        /// </summary>
        private static ImageSource? BuildIconFromGlyphOrNull(ExtensionCatalogItem item)
        {
            // No icon files exist for catalog-only entries yet.
            _ = item;
            return null;
        }

        // ─────────────────────────────────────────────
        //  Actions
        // ─────────────────────────────────────────────

        private void OnDetailsClicked(object sender, RoutedEventArgs e)
        {
            ActionRequested?.Invoke(this, new ExtensionActionEventArgs
            {
                Kind = ExtensionActionKind.Details,
                CatalogItem = Item
            });
        }

        private void OnInstallClicked(object sender, RoutedEventArgs e)
        {
            ActionRequested?.Invoke(this, new ExtensionActionEventArgs
            {
                Kind = ExtensionActionKind.Install,
                CatalogItem = Item
            });
        }
    }
}