using System;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using DIHub.Core.Security;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DIHub.APP.Views
{
    public sealed partial class AddEditServiceDialog : UserControl
    {
        private static readonly (string Glyph, string Label)[] IconChoices =
        {
            ("\uE8F2", "Chat"),
            ("\uE99A", "Idea"),
            ("\uE7C3", "Page"),
            ("\uE8A5", "Doc"),
            ("\uE721", "Search"),
            ("\uE774", "Globe"),
            ("\uE734", "Star"),
            ("\uE9CE", "Grid"),
        };

        private static readonly (AccentColor Accent, string Hex)[] AccentChoices =
        {
            (AccentColor.Purple, "#8B5CF6"),
            (AccentColor.Blue,   "#3B82F6"),
            (AccentColor.Cyan,   "#06B6D4"),
            (AccentColor.Green,  "#22C55E"),
            (AccentColor.Orange, "#F97316"),
        };

        private string _selectedIcon = "\uE8F2";
        private AccentColor _selectedAccent = AccentColor.Purple;

        public AddEditServiceDialog()
        {
            InitializeComponent();
            BuildIconPicker();
            BuildAccentPicker();
        }

        public void LoadService(AIService? service)
        {
            if (service is null)
            {
                NameBox.Text = string.Empty;
                UrlBox.Text = string.Empty;
                _selectedIcon = "\uE8F2";
                _selectedAccent = AccentColor.Purple;
                FavoriteCheck.IsChecked = false;
                ExternalCheck.IsChecked = false;
            }
            else
            {
                NameBox.Text = service.Name;
                UrlBox.Text = service.Url;
                _selectedIcon = service.Icon;
                _selectedAccent = service.Accent;
                FavoriteCheck.IsChecked = service.Favorite;
                ExternalCheck.IsChecked = service.OpenInExternalBrowser;
            }

            RefreshIconSelection();
            RefreshAccentSelection();
            HideError();
        }

        public bool Validate(out string error)
        {
            error = string.Empty;

            var name = NameBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Name is required.";
                return false;
            }

            var url = UrlBox.Text?.Trim() ?? string.Empty;
            if (!UrlPolicy.TryValidateServiceUrl(url, out var urlError))
            {
                error = urlError;
                return false;
            }

            return true;
        }

        public AIService BuildService(AIService? existing = null)
        {
            var service = existing?.Clone() ?? new AIService();

            service.Name = NameBox.Text.Trim();
            service.Url = UrlBox.Text.Trim();
            service.Icon = _selectedIcon;
            service.Accent = _selectedAccent;
            service.Favorite = FavoriteCheck.IsChecked == true;
            service.OpenInExternalBrowser = ExternalCheck.IsChecked == true;

            return service;
        }

        public void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }

        private void BuildIconPicker()
        {
            IconPanel.Children.Clear();

            foreach (var (glyph, label) in IconChoices)
            {
                var icon = new FontIcon { Glyph = glyph, FontSize = 14 };

                var btn = new Button
                {
                    Content = icon,
                    Width = 40,
                    Height = 40,
                    Padding = new Thickness(0),
                    Tag = glyph,
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(1),
                    BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                    CornerRadius = new CornerRadius(6)
                };

                ToolTipService.SetToolTip(btn, label);

                btn.Click += (s, e) =>
                {
                    _selectedIcon = glyph;
                    RefreshIconSelection();
                };

                IconPanel.Children.Add(btn);
            }
        }

        private void RefreshIconSelection()
        {
            var accentBrush = (Brush)Application.Current.Resources["AppAccentBrush"];

            foreach (var child in IconPanel.Children)
            {
                if (child is not Button btn) continue;

                var isSelected = btn.Tag as string == _selectedIcon;
                btn.BorderBrush = isSelected ? accentBrush : (Brush)Application.Current.Resources["AppBorderBrush"];
                btn.BorderThickness = new Thickness(isSelected ? 2 : 1);
            }
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
                    _selectedAccent = accent;
                    RefreshAccentSelection();
                };

                AccentPanel.Children.Add(btn);
            }
        }

        private void RefreshAccentSelection()
        {
            var white = new SolidColorBrush(Colors.White);

            foreach (var child in AccentPanel.Children)
            {
                if (child is not Button btn) continue;
                if (btn.Content is not Border swatch) continue;

                var isSelected = btn.Tag is AccentColor a && a == _selectedAccent;
                swatch.BorderBrush = isSelected ? white : new SolidColorBrush(Colors.Transparent);
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