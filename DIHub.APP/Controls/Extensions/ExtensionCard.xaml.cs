using System;
using System.Linq;
using DIHub.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
            RefreshInstallUi();
        }

        // ─────────────────────────────────────────────
        //  DP callbacks
        //  ─────────────────────────────────────────────

        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtensionCard card) card.Refresh();
        }

        private static void OnIsInstalledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtensionCard card) card.RefreshInstallUi();
        }

        // ─────────────────────────────────────────────
        //  Render
        //  ─────────────────────────────────────────────

        private void Refresh()
        {
            var item = Item;
            if (item is null)
            {
                NameText.Text = "(no item)";
                VersionText.Text = string.Empty;
                DescriptionText.Text = string.Empty;
                CategoryText.Text = string.Empty;
                CategoryBadge.Visibility = Visibility.Collapsed;
                return;
            }

            NameText.Text = item.Name;
            VersionText.Text = string.IsNullOrEmpty(item.Version)
                ? string.Empty
                : "v" + item.Version;
            DescriptionText.Text = item.Description ?? string.Empty;

            var category = item.Categories?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
            CategoryText.Text = string.IsNullOrEmpty(category) ? "General" : category;
            CategoryBadge.Visibility = Visibility.Visible;

            // Icon: glyph-based (no remote image in catalog-only entries).
            var glyph = string.IsNullOrEmpty(item.Glyph) ? "\uE7B8" : item.Glyph!;
            IconGlyph.Glyph = glyph;
            IconImage.Visibility = Visibility.Collapsed;
            IconGlyph.Visibility = Visibility.Visible;

            RefreshInstallUi();
        }

        private void RefreshInstallUi()
        {
            if (InstallButton is null || InstalledIndicator is null) return;

            InstallButton.Visibility = IsInstalled
                ? Visibility.Collapsed
                : Visibility.Visible;

            InstalledIndicator.Visibility = IsInstalled
                ? Visibility.Visible
                : Visibility.Collapsed;
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
            // Extra safety: never fire install when already installed.
            if (IsInstalled) return;

            ActionRequested?.Invoke(this, new ExtensionActionEventArgs
            {
                Kind = ExtensionActionKind.Install,
                CatalogItem = Item
            });
        }
    }
}