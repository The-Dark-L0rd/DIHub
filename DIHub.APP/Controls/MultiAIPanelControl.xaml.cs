using System;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DIHub.APP.Controls
{
    public enum PanelDispatchState
    {
        Idle,
        Pending,
        Sending,
        Sent,
        Failed,
        Skipped,
        Unavailable
    }

    public enum MoveDirection
    {
        Left,
        Right
    }

    public sealed partial class MultiAIPanelControl : UserControl
    {
        private readonly IAIServiceManager _serviceManager;
        private readonly IAIAccountManager _accountManager;
        private readonly IBrowserService _browserService;
        private readonly INotificationService _notifications;

        private WebView2Host? _webHost;
        private string _renderedServiceId = string.Empty;
        private string _renderedAccountId = string.Empty;
        private bool _suppressTargetEvent;

        public MultiAIPanel Panel { get; private set; } = new();
        public AIService? Service { get; private set; }
        public AIAccount? Account { get; private set; }

        public event EventHandler<MultiAIPanel>? ChooseRequested;
        public event EventHandler<MultiAIPanel>? RemoveRequested;
        public event EventHandler<MultiAIPanel>? MaximizeRequested;
        public event EventHandler<MultiAIPanel>? Activated;
        public event EventHandler<(MultiAIPanel Panel, bool IsTarget)>? TargetChanged;
        public event EventHandler<(MultiAIPanel Panel, MoveDirection Direction)>? MoveRequested;

        public MultiAIPanelControl()
        {
            InitializeComponent();

            _serviceManager = App.GetService<IAIServiceManager>();
            _accountManager = App.GetService<IAIAccountManager>();
            _browserService = App.GetService<IBrowserService>();
            _notifications = App.GetService<INotificationService>();

            Tapped += (s, e) => Activated?.Invoke(this, Panel);
        }

        public void SetPanel(MultiAIPanel panel, AIService? service, AIAccount? account)
        {
            Panel = panel;
            Service = service;
            Account = account;

            var newServiceId = service?.Id ?? string.Empty;
            var newAccountId = account?.Id ?? string.Empty;

            var serviceChanged = _renderedServiceId != newServiceId;
            var accountChanged = _renderedAccountId != newAccountId;
            var needsRebuild = serviceChanged || accountChanged || _webHost is null;

            _renderedServiceId = newServiceId;
            _renderedAccountId = newAccountId;

            Refresh(needsRebuild);
        }

        private void Refresh(bool needsRebuild)
        {
            _suppressTargetEvent = true;
            TargetCheck.IsChecked = Panel.IsPromptTarget;
            _suppressTargetEvent = false;

            MaximizeIcon.Glyph = Panel.IsMaximized ? "\uE73F" : "\uE740";
            UpdateActiveVisual();

            if (Service is not null && Account is not null)
            {
                EmptyState.Visibility = Visibility.Collapsed;
                WebHostContainer.Visibility = Visibility.Visible;
                TargetCheck.IsEnabled = true;

                HeaderIcon.Glyph = Account.Icon;
                HeaderTitle.Text = $"{Service.Name} · {Account.Name}";

                if (needsRebuild) RebuildWebHost();
            }
            else
            {
                EmptyState.Visibility = Visibility.Visible;
                WebHostContainer.Visibility = Visibility.Collapsed;
                TargetCheck.IsEnabled = false;
                TargetCheck.IsChecked = false;

                HeaderIcon.Glyph = "\uE774";
                HeaderTitle.Text = "Empty Panel";

                if (needsRebuild) DisposeWebHost();
            }
        }

        private void UpdateActiveVisual()
        {
            var accentBrush = (Application.Current.Resources["AppAccentBrush"] as Brush)
                              ?? new SolidColorBrush(Colors.Purple);
            var borderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];

            if (Panel.IsActive)
            {
                RootBorder.BorderBrush = accentBrush;
                RootBorder.BorderThickness = new Thickness(2);
            }
            else
            {
                RootBorder.BorderBrush = borderBrush;
                RootBorder.BorderThickness = new Thickness(1);
            }
        }

        private void OnTargetCheckChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressTargetEvent) return;
            if (Service is null || Account is null) return;

            Panel.IsPromptTarget = TargetCheck.IsChecked == true;
            TargetChanged?.Invoke(this, (Panel, Panel.IsPromptTarget));
        }

        private void OnHeaderDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MaximizeRequested?.Invoke(this, Panel);
        }

        private void OnMaximizeClicked(object sender, RoutedEventArgs e)
        {
            MaximizeRequested?.Invoke(this, Panel);
        }

        private void OnMoveLeftClicked(object sender, RoutedEventArgs e)
        {
            MoveRequested?.Invoke(this, (Panel, MoveDirection.Left));
        }

        private void OnMoveRightClicked(object sender, RoutedEventArgs e)
        {
            MoveRequested?.Invoke(this, (Panel, MoveDirection.Right));
        }

        public void SetDispatchState(PanelDispatchState state, string? customText = null)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (state)
                {
                    case PanelDispatchState.Idle:
                        StatusChip.Visibility = Visibility.Collapsed;
                        DispatchOverlay.Visibility = Visibility.Collapsed;
                        break;
                    case PanelDispatchState.Pending:
                        SetStatus("Pending", Color.FromArgb(255, 0x6B, 0x72, 0x80));
                        DispatchOverlay.Visibility = Visibility.Collapsed;
                        break;
                    case PanelDispatchState.Sending:
                        SetStatus("Sending", Color.FromArgb(255, 0x8B, 0x5C, 0xF6));
                        DispatchOverlayText.Text = "Sending...";
                        DispatchOverlay.Visibility = Visibility.Visible;
                        break;
                    case PanelDispatchState.Sent:
                        SetStatus("Sent", Color.FromArgb(255, 0x22, 0xC5, 0x5E));
                        DispatchOverlay.Visibility = Visibility.Collapsed;
                        break;
                    case PanelDispatchState.Failed:
                        SetStatus("Failed", Color.FromArgb(255, 0xEF, 0x44, 0x44));
                        DispatchOverlay.Visibility = Visibility.Collapsed;
                        break;
                    case PanelDispatchState.Skipped:
                        SetStatus("Skipped", Color.FromArgb(255, 0x6B, 0x72, 0x80));
                        DispatchOverlay.Visibility = Visibility.Collapsed;
                        break;
                    case PanelDispatchState.Unavailable:
                        SetStatus("Manual", Color.FromArgb(255, 0xF9, 0x73, 0x16));
                        DispatchOverlay.Visibility = Visibility.Collapsed;
                        break;
                }

                if (customText is not null && state != PanelDispatchState.Idle)
                    StatusText.Text = customText;
            });
        }

        private void SetStatus(string text, Color color)
        {
            StatusText.Text = text;
            StatusChip.Background = new SolidColorBrush(color);
            StatusChip.Visibility = Visibility.Visible;
        }

        private void RebuildWebHost()
        {
            if (Service is null || Account is null) return;

            DisposeWebHost();

            try
            {
                var host = new WebView2Host();
                var profilePath = _accountManager.GetProfilePath(Account.Id);
                host.SetProfilePath(profilePath);

                _webHost = host;
                WebHostContainer.Children.Add(host);
                host.Navigate(Service.Url);
            }
            catch (Exception ex)
            {
                _notifications.Show("Panel Error", ex.Message, NotificationSeverity.Error);
            }
        }

        private void DisposeWebHost()
        {
            if (_webHost is null) return;
            try { _webHost.Dispose(); } catch { }
            try { WebHostContainer.Children.Remove(_webHost); } catch { }
            _webHost = null;
        }

        public WebView2Host? GetWebHost() => _webHost;

        private void OnChooseClicked(object sender, RoutedEventArgs e)
            => ChooseRequested?.Invoke(this, Panel);

        private void OnRemoveClicked(object sender, RoutedEventArgs e)
            => RemoveRequested?.Invoke(this, Panel);

        private void OnReloadClicked(object sender, RoutedEventArgs e)
        {
            try { _webHost?.Reload(); } catch { }
        }

        private void OnHardReloadClicked(object sender, RoutedEventArgs e)
        {
            if (Service is null) return;
            try { _webHost?.Navigate(Service.Url); } catch { }
        }

        private void OnOpenExternalClicked(object sender, RoutedEventArgs e)
        {
            if (Service is null) return;
            _browserService.OpenInDefaultBrowser(Service.Url);
        }

        private async void OnClearSessionClicked(object sender, RoutedEventArgs e)
        {
            if (Account is null) return;

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Clear Session",
                Content = $"Clear cookies, cache and login for \"{Account.Name}\"? Other accounts of this service are not affected.",
                PrimaryButtonText = "Clear",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            await _accountManager.ClearProfileAsync(Account.Id);
            RebuildWebHost();
            _notifications.Show("Session Cleared", Account.Name, NotificationSeverity.Success);
        }

        public void Dispose()
        {
            DisposeWebHost();
        }
    }
}