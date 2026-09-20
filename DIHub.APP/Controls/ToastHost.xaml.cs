using System;
using System.Collections.Generic;
using DIHub.Core.Interfaces;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace DIHub.APP.Controls
{
    public sealed partial class ToastHost : UserControl
    {
        private readonly INotificationService _notificationService;
        private readonly Queue<FrameworkElement> _activeToasts = new();
        private const int MaxVisibleToasts = 4;
        private const int AutoDismissMs = 4000;

        public ToastHost()
        {
            InitializeComponent();

            _notificationService = App.GetService<INotificationService>();
            _notificationService.NotificationRequested += OnNotificationRequested;
        }

        private void OnNotificationRequested(object? sender, NotificationEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() => ShowToast(e));
        }

        private void ShowToast(NotificationEventArgs e)
        {
            // Limit visible toasts
            while (_activeToasts.Count >= MaxVisibleToasts)
            {
                var oldest = _activeToasts.Dequeue();
                ToastStack.Children.Remove(oldest);
            }

            var accentColor = e.Severity switch
            {
                NotificationSeverity.Success => Color.FromArgb(255, 0x22, 0xC5, 0x5E),
                NotificationSeverity.Warning => Color.FromArgb(255, 0xF9, 0x73, 0x16),
                NotificationSeverity.Error => Color.FromArgb(255, 0xEF, 0x44, 0x44),
                _ => Color.FromArgb(255, 0x8B, 0x5C, 0xF6)
            };

            var iconGlyph = e.Severity switch
            {
                NotificationSeverity.Success => "\uE73E",
                NotificationSeverity.Warning => "\uE7BA",
                NotificationSeverity.Error => "\uEA39",
                _ => "\uE946"
            };

            // ── Build the toast ──
            var iconBlock = new FontIcon
            {
                Glyph = iconGlyph,
                FontSize = 16,
                Foreground = new SolidColorBrush(accentColor),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var titleBlock = new TextBlock
            {
                Text = e.Title,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };

            var msgBlock = new TextBlock
            {
                Text = e.Message,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.75,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var textStack = new StackPanel
            {
                Spacing = 2,
                VerticalAlignment = VerticalAlignment.Center
            };
            textStack.Children.Add(titleBlock);
            if (!string.IsNullOrWhiteSpace(e.Message))
                textStack.Children.Add(msgBlock);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(iconBlock, 0);
            Grid.SetColumn(textStack, 1);
            textStack.Margin = new Thickness(12, 0, 0, 0);
            grid.Children.Add(iconBlock);
            grid.Children.Add(textStack);

            var border = new Border
            {
                Background = Application.Current.Resources["AppSurfaceBrush"] as Brush
                             ?? new SolidColorBrush(Color.FromArgb(255, 20, 20, 22)),
                BorderBrush = Application.Current.Resources["AppBorderBrush"] as Brush
                             ?? new SolidColorBrush(Color.FromArgb(255, 40, 40, 45)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12),
                MinWidth = 280,
                Opacity = 0,
                RenderTransform = new CompositeTransform { TranslateX = 40 }
            };
            border.Child = grid;
            border.Shadow = new Microsoft.UI.Xaml.Media.ThemeShadow();

            // ── Add to stack ──
            ToastStack.Children.Add(border);
            _activeToasts.Enqueue(border);

            // ── Fade-in animation ──
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(220)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var slideIn = new DoubleAnimation
            {
                From = 40,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(260)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var sb = new Storyboard();
            Storyboard.SetTarget(fadeIn, border);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");
            Storyboard.SetTarget(slideIn, border);
            Storyboard.SetTargetProperty(slideIn, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
            sb.Children.Add(fadeIn);
            sb.Children.Add(slideIn);
            sb.Begin();

            // ── Auto dismiss ──
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AutoDismissMs) };
            timer.Tick += (s, _) =>
            {
                timer.Stop();
                DismissToast(border);
            };
            timer.Start();

            border.Tapped += (s, _) =>
            {
                timer.Stop();
                DismissToast(border);
            };
            border.IsHitTestVisible = true;
        }

        private void DismissToast(FrameworkElement toast)
        {
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(180)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var slideOut = new DoubleAnimation
            {
                To = 40,
                Duration = new Duration(TimeSpan.FromMilliseconds(220)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var sb = new Storyboard();
            Storyboard.SetTarget(fadeOut, toast);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");
            Storyboard.SetTarget(slideOut, toast);
            Storyboard.SetTargetProperty(slideOut, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");

            sb.Children.Add(fadeOut);
            sb.Children.Add(slideOut);
            sb.Completed += (s, _) =>
            {
                ToastStack.Children.Remove(toast);
                _activeToasts.Clear(); // simple reset (queue may be out of sync, but harmless)
            };
            sb.Begin();
        }
    }
}