using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIHub.APP.Controls;
using DIHub.APP.Services;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI;

namespace DIHub.APP.Views
{
    public sealed partial class MultiAIView : UserControl
    {
        private readonly IMultiAIWorkspaceManager _manager;
        private readonly IAIServiceManager _serviceManager;
        private readonly INotificationService _notifications;
        private readonly IPresetManager _presets;
        private readonly ISettingsService _settings;
        private readonly PromptDispatcher _dispatcher;

        private bool _loading;
        private readonly Dictionary<string, MultiAIPanelControl> _livePanels = new();
        private CancellationTokenSource? _broadcastCts;

        private string? _lastPrompt;
        private readonly HashSet<string> _lastFailedPanelIds = new();
        private int _lastSentCount = 0;
        private int _lastManualCount = 0;
        private int _lastFailedCount = 0;

        public event EventHandler? RequestClose;

        public MultiAIView()
        {
            InitializeComponent();

            _manager = App.GetService<IMultiAIWorkspaceManager>();
            _serviceManager = App.GetService<IAIServiceManager>();
            _notifications = App.GetService<INotificationService>();
            _presets = App.GetService<IPresetManager>();
            _settings = App.GetService<ISettingsService>();
            _dispatcher = App.GetService<PromptDispatcher>();

            KeyDown += OnKeyDown;
        }

        // ─────────────────────────────────────────────
        //  Open / Close
        // ─────────────────────────────────────────────

        public void Open()
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null)
            {
                var defaultCount = _settings.Current.MultiAIDefaultPanelCount;
                if (defaultCount < 2) defaultCount = 2;
                if (defaultCount > 4) defaultCount = 4;

                ws = _manager.CreateWorkspace(defaultCount, "Multi-AI");
                EnsurePanelCount(ws, defaultCount);

                if (_settings.Current.MultiAIKeepTargetsForNext)
                    ws.KeepTargetsForNext = true;

                _ = _manager.SaveAsync();
            }

            Visibility = Visibility.Visible;
            _loading = true;
            LayoutCombo.SelectedIndex = ws.PanelCount switch { 3 => 1, 4 => 2, _ => 0 };
            KeepTargetsCheck.IsChecked = ws.KeepTargetsForNext;
            _loading = false;

            if (ws.ActivePanelId is null && ws.Panels.Count > 0)
                ws.ActivePanelId = ws.Panels[0].Id;

            SyncPanelGrid();
            RefreshTargetChips();
            UpdateComposerStatus();
            BuildPresetsMenu();
            RefreshHistoryList();
            Focus(FocusState.Programmatic);
        }

        public void Close()
        {
            CancelBroadcast();
            DisposeAllPanels();
            Visibility = Visibility.Collapsed;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void DisposeAllPanels()
        {
            foreach (var ctrl in _livePanels.Values)
            {
                try { ctrl.Dispose(); } catch { }
            }
            _livePanels.Clear();
            PanelGrid.Children.Clear();
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                if (_broadcastCts is not null) { e.Handled = true; CancelBroadcast(); }
                else { e.Handled = true; Close(); }
                return;
            }

            var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            var isCtrl = ctrlState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var isShift = shiftState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (isCtrl && !isShift)
            {
                int index = e.Key switch
                {
                    VirtualKey.Number1 => 0,
                    VirtualKey.Number2 => 1,
                    VirtualKey.Number3 => 2,
                    VirtualKey.Number4 => 3,
                    _ => -1
                };

                if (index >= 0)
                {
                    e.Handled = true;
                    ActivatePanelByIndex(index);
                    return;
                }

                if (e.Key == VirtualKey.M)
                {
                    e.Handled = true;
                    var ws = _manager.ActiveWorkspace;
                    var activePanel = ws?.Panels.FirstOrDefault(p => p.Id == ws.ActivePanelId);
                    if (activePanel is not null)
                        ToggleMaximize(activePanel);
                    return;
                }

                if (e.Key == VirtualKey.R)
                {
                    e.Handled = true;
                    ResetLayout();
                    return;
                }
            }

            if (isCtrl && e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
                CycleActivePanel(reverse: isShift);
                return;
            }
        }

        // ─────────────────────────────────────────────
        //  Layout
        // ─────────────────────────────────────────────

        private void OnLayoutChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            var item = LayoutCombo.SelectedItem as ComboBoxItem;
            if (item?.Tag is not string tagStr) return;
            if (!int.TryParse(tagStr, out var newCount)) return;

            foreach (var p in ws.Panels) p.IsMaximized = false;

            EnsurePanelCount(ws, newCount);
            _ = _manager.SaveAsync();

            SyncPanelGrid();
            RefreshTargetChips();
            UpdateComposerStatus();
        }

        private void EnsurePanelCount(MultiAIWorkspace ws, int count)
        {
            if (count < 2) count = 2;
            if (count > 4) count = 4;

            while (ws.Panels.Count < count)
                ws.Panels.Add(new MultiAIPanel { Order = ws.Panels.Count, IsPromptTarget = true });

            while (ws.Panels.Count > count)
                ws.Panels.RemoveAt(ws.Panels.Count - 1);

            for (int i = 0; i < ws.Panels.Count; i++) ws.Panels[i].Order = i;

            ws.PanelCount = count;
            ws.LayoutMode = _manager.ComputeLayout(count);
        }

        // ─────────────────────────────────────────────
        //  Grid sync
        // ─────────────────────────────────────────────

        private void SyncPanelGrid()
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            var currentIds = ws.Panels.Select(p => p.Id).ToHashSet();

            var removedIds = _livePanels.Keys.Where(id => !currentIds.Contains(id)).ToList();
            foreach (var id in removedIds)
            {
                if (_livePanels.TryGetValue(id, out var ctrl))
                {
                    try { ctrl.Dispose(); } catch { }
                    PanelGrid.Children.Remove(ctrl);
                    _livePanels.Remove(id);
                }
            }

            foreach (var panel in ws.Panels)
            {
                var service = _serviceManager.FindService(panel.ServiceId);
                var account = service?.Accounts.FirstOrDefault(a => a.Id == panel.AccountId);

                if (_livePanels.TryGetValue(panel.Id, out var existing))
                {
                    existing.SetPanel(panel, service, account);
                }
                else
                {
                    var newCtrl = new MultiAIPanelControl();
                    newCtrl.SetPanel(panel, service, account);
                    newCtrl.ChooseRequested += OnPanelChooseRequested;
                    newCtrl.RemoveRequested += OnPanelRemoveRequested;
                    newCtrl.TargetChanged += OnPanelTargetChanged;
                    newCtrl.MaximizeRequested += OnPanelMaximizeRequested;
                    newCtrl.Activated += OnPanelActivated;
                    newCtrl.MoveRequested += OnPanelMoveRequested;
                    _livePanels[panel.Id] = newCtrl;
                    PanelGrid.Children.Add(newCtrl);
                }
            }

            ApplyGridLayout(ws);
        }

        private void ApplyGridLayout(MultiAIWorkspace ws)
        {
            PanelGrid.ColumnDefinitions.Clear();
            PanelGrid.RowDefinitions.Clear();

            var maximized = ws.Panels.FirstOrDefault(p => p.IsMaximized);

            if (maximized is not null)
            {
                PanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                PanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                foreach (var panel in ws.Panels)
                {
                    if (!_livePanels.TryGetValue(panel.Id, out var ctrl)) continue;
                    var isMax = panel.Id == maximized.Id;

                    ctrl.Visibility = isMax ? Visibility.Visible : Visibility.Collapsed;
                    if (isMax)
                    {
                        Grid.SetRow(ctrl, 0);
                        Grid.SetColumn(ctrl, 0);
                        Grid.SetRowSpan(ctrl, 1);
                        Grid.SetColumnSpan(ctrl, 1);
                    }
                }
                return;
            }

            foreach (var ctrl in _livePanels.Values)
                ctrl.Visibility = Visibility.Visible;

            var count = ws.Panels.Count;
            if (count == 0) count = 2;

            switch (count)
            {
                case 2:
                    PanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    PanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    PanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    break;
                case 3:
                    PanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    PanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    PanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    PanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    break;
                case 4:
                    PanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    PanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    PanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    PanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    break;
            }

            for (int i = 0; i < ws.Panels.Count; i++)
            {
                var panel = ws.Panels[i];
                if (!_livePanels.TryGetValue(panel.Id, out var ctrl)) continue;

                var (row, col, rowSpan, colSpan) = GetPanelPosition(i, count);
                Grid.SetRow(ctrl, row);
                Grid.SetColumn(ctrl, col);
                Grid.SetRowSpan(ctrl, rowSpan);
                Grid.SetColumnSpan(ctrl, colSpan);
            }
        }

        private static (int row, int col, int rowSpan, int colSpan) GetPanelPosition(int index, int count)
        {
            return count switch
            {
                2 => (0, index, 1, 1),
                3 => index switch
                {
                    0 => (0, 0, 1, 1),
                    1 => (0, 1, 1, 1),
                    2 => (1, 0, 1, 2),
                    _ => (0, 0, 1, 1)
                },
                4 => (index / 2, index % 2, 1, 1),
                _ => (0, 0, 1, 1)
            };
        }

        // ─────────────────────────────────────────────
        //  Move panel
        // ─────────────────────────────────────────────

        private void OnPanelMoveRequested(object? sender, (MultiAIPanel Panel, MoveDirection Direction) e)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            int idx = -1;
            for (int i = 0; i < ws.Panels.Count; i++)
            {
                if (ws.Panels[i].Id == e.Panel.Id) { idx = i; break; }
            }
            if (idx < 0) return;

            int newIdx = e.Direction == MoveDirection.Left ? idx - 1 : idx + 1;
            if (newIdx < 0 || newIdx >= ws.Panels.Count) return;

            var temp = ws.Panels[newIdx];
            ws.Panels[newIdx] = ws.Panels[idx];
            ws.Panels[idx] = temp;

            for (int i = 0; i < ws.Panels.Count; i++) ws.Panels[i].Order = i;

            _ = _manager.SaveAsync();
            ApplyGridLayout(ws);
        }

        // ─────────────────────────────────────────────
        //  Maximize
        // ─────────────────────────────────────────────

        private void OnPanelMaximizeRequested(object? sender, MultiAIPanel panel)
            => ToggleMaximize(panel);

        private void ToggleMaximize(MultiAIPanel panel)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            var wasMax = panel.IsMaximized;

            foreach (var p in ws.Panels) p.IsMaximized = false;

            if (!wasMax) panel.IsMaximized = true;

            ws.ActivePanelId = panel.Id;
            UpdateActiveFlags(ws);

            _ = _manager.SaveAsync();
            ApplyGridLayout(ws);
            RefreshPanelHeaders();
        }

        private void ResetLayout()
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            foreach (var p in ws.Panels)
            {
                p.IsMaximized = false;
                p.WidthRatio = 1.0;
                p.HeightRatio = 1.0;
            }

            _ = _manager.SaveAsync();
            ApplyGridLayout(ws);
            RefreshPanelHeaders();

            _notifications.Show("Layout Reset", "Panel sizes restored.", NotificationSeverity.Information);
        }

        // ─────────────────────────────────────────────
        //  Active panel
        // ─────────────────────────────────────────────

        private void OnPanelActivated(object? sender, MultiAIPanel panel)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;
            if (ws.ActivePanelId == panel.Id) return;

            ws.ActivePanelId = panel.Id;
            UpdateActiveFlags(ws);
            _ = _manager.SaveAsync();
            RefreshPanelHeaders();
        }

        private void UpdateActiveFlags(MultiAIWorkspace ws)
        {
            foreach (var p in ws.Panels)
                p.IsActive = p.Id == ws.ActivePanelId;
        }

        private void RefreshPanelHeaders()
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            foreach (var panel in ws.Panels)
            {
                if (_livePanels.TryGetValue(panel.Id, out var ctrl))
                {
                    var svc = _serviceManager.FindService(panel.ServiceId);
                    var acc = svc?.Accounts.FirstOrDefault(a => a.Id == panel.AccountId);
                    ctrl.SetPanel(panel, svc, acc);
                }
            }
        }

        private void ActivatePanelByIndex(int index)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;
            if (index < 0 || index >= ws.Panels.Count) return;

            ws.ActivePanelId = ws.Panels[index].Id;
            UpdateActiveFlags(ws);
            _ = _manager.SaveAsync();
            RefreshPanelHeaders();
        }

        private void CycleActivePanel(bool reverse)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null || ws.Panels.Count == 0) return;

            int currentIdx = -1;
            for (int i = 0; i < ws.Panels.Count; i++)
            {
                if (ws.Panels[i].Id == ws.ActivePanelId) { currentIdx = i; break; }
            }

            if (currentIdx < 0) currentIdx = 0;

            var nextIdx = reverse
                ? (currentIdx - 1 + ws.Panels.Count) % ws.Panels.Count
                : (currentIdx + 1) % ws.Panels.Count;

            ws.ActivePanelId = ws.Panels[nextIdx].Id;
            UpdateActiveFlags(ws);
            _ = _manager.SaveAsync();
            RefreshPanelHeaders();
        }

        // ─────────────────────────────────────────────
        //  Panel events
        // ─────────────────────────────────────────────

        private async void OnPanelChooseRequested(object? sender, MultiAIPanel panel)
            => await ShowPickerAsync(panel);

        private void OnPanelRemoveRequested(object? sender, MultiAIPanel panel)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            if (ws.Panels.Count <= 2)
            {
                _notifications.Show("Cannot Remove",
                    "A Multi-AI Workspace must have at least 2 panels.",
                    NotificationSeverity.Warning);
                return;
            }

            ws.Panels.Remove(panel);
            for (int i = 0; i < ws.Panels.Count; i++) ws.Panels[i].Order = i;
            ws.PanelCount = ws.Panels.Count;
            ws.LayoutMode = _manager.ComputeLayout(ws.PanelCount);

            if (ws.ActivePanelId == panel.Id)
                ws.ActivePanelId = ws.Panels.FirstOrDefault()?.Id;

            _ = _manager.SaveAsync();

            _loading = true;
            LayoutCombo.SelectedIndex = ws.PanelCount switch { 3 => 1, 4 => 2, _ => 0 };
            _loading = false;

            SyncPanelGrid();
            RefreshTargetChips();
            UpdateComposerStatus();
        }

        private void OnPanelTargetChanged(object? sender, (MultiAIPanel Panel, bool IsTarget) e)
        {
            _ = _manager.SaveAsync();
            RefreshTargetChips();
            UpdateComposerStatus();
        }

        // ─────────────────────────────────────────────
        //  Duplicate account guard
        // ─────────────────────────────────────────────

        private bool IsDuplicateAccount(MultiAIPanel target, string accountId)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return false;

            foreach (var p in ws.Panels)
            {
                if (p.Id == target.Id) continue;
                if (p.AccountId == accountId && !string.IsNullOrEmpty(accountId))
                    return true;
            }
            return false;
        }

        private async Task ShowPickerAsync(MultiAIPanel panel)
        {
            var picker = new PanelTargetPickerDialog();

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Choose AI for Panel",
                Content = picker,
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.None
            };

            picker.AccountSelected += async (s, account) =>
            {
                var svc = _serviceManager.FindService(account.ServiceId);
                if (svc is null) return;

                if (IsDuplicateAccount(panel, account.Id))
                {
                    dialog.Hide();

                    var dupDialog = new ContentDialog
                    {
                        XamlRoot = RootGrid.XamlRoot,
                        Title = "Account Already in Use",
                        Content = $"\"{svc.Name} · {account.Name}\" is already open in another panel. " +
                                  "To keep each account's session isolated, the same account can only be used in one panel at a time.",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close
                    };
                    await dupDialog.ShowAsync();
                    return;
                }

                panel.ServiceId = svc.Id;
                panel.AccountId = account.Id;
                _ = _manager.SaveAsync();
                dialog.Hide();
                SyncPanelGrid();
                RefreshTargetChips();
                UpdateComposerStatus();
            };

            await dialog.ShowAsync();
        }

        // ─────────────────────────────────────────────
        //  Target chips
        // ─────────────────────────────────────────────

        private void RefreshTargetChips()
        {
            TargetChipsPanel.Children.Clear();

            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            foreach (var panel in ws.Panels)
            {
                var svc = _serviceManager.FindService(panel.ServiceId);
                var acc = svc?.Accounts.FirstOrDefault(a => a.Id == panel.AccountId);

                string label = (svc is null || acc is null) ? "Empty" : $"{svc.Name} · {acc.Name}";
                TargetChipsPanel.Children.Add(BuildTargetChip(panel, label, svc is not null && acc is not null));
            }
        }

        private Button BuildTargetChip(MultiAIPanel panel, string label, bool enabled)
        {
            var accentBrush = (Application.Current.Resources["AppAccentBrush"] as Brush)
                              ?? new SolidColorBrush(Colors.Purple);

            var textBrush = enabled
                ? (Brush)Application.Current.Resources["AppTextPrimaryBrush"]
                : (Brush)Application.Current.Resources["AppTextSecondaryBrush"];

            var borderBrush = panel.IsPromptTarget && enabled
                ? accentBrush
                : (Brush)Application.Current.Resources["AppBorderBrush"];

            var bgBrush = panel.IsPromptTarget && enabled
                ? new SolidColorBrush(Color.FromArgb(30, 0x8B, 0x5C, 0xF6))
                : new SolidColorBrush(Colors.Transparent);

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

            row.Children.Add(new FontIcon
            {
                Glyph = panel.IsPromptTarget ? "\uE73E" : "\uE739",
                FontSize = 10,
                Foreground = enabled && panel.IsPromptTarget ? accentBrush : textBrush,
                VerticalAlignment = VerticalAlignment.Center
            });

            row.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = textBrush,
                VerticalAlignment = VerticalAlignment.Center
            });

            var button = new Button
            {
                Content = row,
                Tag = panel,
                Padding = new Thickness(10, 5, 10, 5),
                Background = bgBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                IsEnabled = enabled
            };

            button.Click += (s, e) =>
            {
                if (!enabled) return;
                panel.IsPromptTarget = !panel.IsPromptTarget;
                _ = _manager.SaveAsync();
                RefreshTargetChips();
                UpdateComposerStatus();
                RefreshAllPanelCheckboxes();
            };

            return button;
        }

        private void OnTargetsAllClicked(object sender, RoutedEventArgs e)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;
            foreach (var p in ws.Panels) p.IsPromptTarget = true;
            _ = _manager.SaveAsync();
            RefreshTargetChips();
            UpdateComposerStatus();
            RefreshAllPanelCheckboxes();
        }

        private void OnTargetsNoneClicked(object sender, RoutedEventArgs e)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;
            foreach (var p in ws.Panels) p.IsPromptTarget = false;
            _ = _manager.SaveAsync();
            RefreshTargetChips();
            UpdateComposerStatus();
            RefreshAllPanelCheckboxes();
        }

        private void RefreshAllPanelCheckboxes()
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            foreach (var panel in ws.Panels)
            {
                if (_livePanels.TryGetValue(panel.Id, out var ctrl))
                {
                    var svc = _serviceManager.FindService(panel.ServiceId);
                    var acc = svc?.Accounts.FirstOrDefault(a => a.Id == panel.AccountId);
                    ctrl.SetPanel(panel, svc, acc);
                }
            }
        }

        private void UpdateComposerStatus()
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            var validTargets = ws.Panels.Count(p => p.IsPromptTarget && !string.IsNullOrEmpty(p.AccountId));
            var totalPanels = ws.Panels.Count(p => !string.IsNullOrEmpty(p.AccountId));

            if (totalPanels == 0)
            {
                ComposerStatus.Text = "Choose AI for at least one panel to get started.";
                SendButtonText.Text = "Send";
                SendButton.IsEnabled = false;
                return;
            }

            if (validTargets == 0)
            {
                ComposerStatus.Text = "No targets selected. Click a chip above to add a target.";
                SendButtonText.Text = "Send";
                SendButton.IsEnabled = false;
                return;
            }

            ComposerStatus.Text = $"Ready to send to {validTargets} of {totalPanels} panel(s).";
            SendButtonText.Text = $"Send to {validTargets}";
            SendButton.IsEnabled = true;
        }

        // ─────────────────────────────────────────────
        //  Composer
        // ─────────────────────────────────────────────

        private void OnKeepTargetsChanged(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;
            ws.KeepTargetsForNext = KeepTargetsCheck.IsChecked == true;
            _ = _manager.SaveAsync();
        }

        private void OnPromptKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !IsShiftDown())
            {
                e.Handled = true;
                OnSendClicked(sender, new RoutedEventArgs());
            }
        }

        private static bool IsShiftDown()
        {
            var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            return shift.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        }

        private async void OnSendClicked(object sender, RoutedEventArgs e)
        {
            var text = PromptInput.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return;

            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            var targets = ws.Panels
                .Where(p => p.IsPromptTarget && !string.IsNullOrEmpty(p.AccountId))
                .ToList();

            if (targets.Count == 0) return;

            // ── Confirm multi-send (respect setting) ──
            if (targets.Count > 1 && _settings.Current.MultiAIConfirmMultiSend)
            {
                var confirmed = await ConfirmMultiSendAsync(targets);
                if (!confirmed) return;
            }

            await DispatchBroadcastAsync(text, targets);

            _presets.AddHistory(
                text,
                targets.Select(t => new TargetRef { ServiceId = t.ServiceId, AccountId = t.AccountId }),
                targets.Select(p => GetPanelLabel(p)),
                _lastSentCount,
                _lastFailedCount);

            RefreshHistoryList();
            PromptInput.Text = string.Empty;
        }

        private async Task<bool> ConfirmMultiSendAsync(List<MultiAIPanel> targets)
        {
            var list = new StackPanel { Spacing = 6, MinWidth = 320 };

            foreach (var t in targets)
            {
                var label = GetPanelLabel(t);
                list.Children.Add(new TextBlock
                {
                    Text = "• " + label,
                    FontSize = 13
                });
            }

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = $"Send to {targets.Count} AIs?",
                Content = list,
                PrimaryButtonText = $"Send to {targets.Count}",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) => CancelBroadcast();
        private void CancelBroadcast() => _broadcastCts?.Cancel();

        // ─────────────────────────────────────────────
        //  Broadcast
        // ─────────────────────────────────────────────

        private async Task DispatchBroadcastAsync(string prompt, List<MultiAIPanel> targets, bool isRetry = false)
        {
            _broadcastCts?.Dispose();
            _broadcastCts = new CancellationTokenSource();
            var token = _broadcastCts.Token;

            CancelButton.Visibility = Visibility.Visible;
            SendButton.IsEnabled = false;
            RetryFailedButton.Visibility = Visibility.Collapsed;

            if (!isRetry)
            {
                _lastPrompt = prompt;
                _lastFailedPanelIds.Clear();
            }

            int sentCount = 0;
            int manualCount = 0;
            int failedCount = 0;
            var currentFailedIds = new HashSet<string>();

            try
            {
                foreach (var panel in targets)
                {
                    if (_livePanels.TryGetValue(panel.Id, out var ctrl))
                        ctrl.SetDispatchState(PanelDispatchState.Pending);
                }

                foreach (var panel in targets)
                {
                    if (token.IsCancellationRequested) break;
                    if (!_livePanels.TryGetValue(panel.Id, out var ctrl)) continue;

                    ctrl.SetDispatchState(PanelDispatchState.Sending);

                    var svc = _serviceManager.FindService(panel.ServiceId);
                    var url = svc?.Url ?? string.Empty;
                    var core = ctrl.GetWebHost()?.Core;

                    var result = await _dispatcher.SendAsync(core!, url, prompt, token);

                    switch (result.Status)
                    {
                        case PromptDispatchStatus.Sent:
                            ctrl.SetDispatchState(PanelDispatchState.Sent);
                            sentCount++;
                            break;
                        case PromptDispatchStatus.InputFound:
                            ctrl.SetDispatchState(PanelDispatchState.Unavailable, "Press Send");
                            manualCount++;
                            currentFailedIds.Add(panel.Id);
                            break;
                        case PromptDispatchStatus.Unavailable:
                            ctrl.SetDispatchState(PanelDispatchState.Unavailable, "Manual");
                            manualCount++;
                            currentFailedIds.Add(panel.Id);
                            break;
                        case PromptDispatchStatus.Cancelled:
                            ctrl.SetDispatchState(PanelDispatchState.Skipped);
                            break;
                        case PromptDispatchStatus.Failed:
                        default:
                            ctrl.SetDispatchState(PanelDispatchState.Failed);
                            failedCount++;
                            currentFailedIds.Add(panel.Id);
                            break;
                    }

                    try { await Task.Delay(120, token); } catch (OperationCanceledException) { }
                }

                _lastSentCount = sentCount;
                _lastManualCount = manualCount;
                _lastFailedCount = failedCount;

                _lastFailedPanelIds.Clear();
                foreach (var id in currentFailedIds) _lastFailedPanelIds.Add(id);

                _notifications.Show(
                    isRetry ? "Retry Finished" : "Broadcast Finished",
                    $"{sentCount} sent · {manualCount} manual · {failedCount} failed",
                    sentCount > 0 ? NotificationSeverity.Success : NotificationSeverity.Information);
            }
            catch (OperationCanceledException)
            {
                _notifications.Show("Broadcast Cancelled",
                    "Pending targets were stopped.",
                    NotificationSeverity.Warning);
            }
            finally
            {
                CancelButton.Visibility = Visibility.Collapsed;
                SendButton.IsEnabled = true;
                _broadcastCts?.Dispose();
                _broadcastCts = null;

                UpdateRetryButton();
                _ = ClearStatusesAfterDelayAsync(3500);
            }
        }

        private async Task ClearStatusesAfterDelayAsync(int ms)
        {
            await Task.Delay(ms);
            foreach (var ctrl in _livePanels.Values)
                ctrl.SetDispatchState(PanelDispatchState.Idle);
        }

        private void UpdateRetryButton()
        {
            if (_lastFailedPanelIds.Count > 0 && !string.IsNullOrWhiteSpace(_lastPrompt))
            {
                RetryFailedText.Text = $"Retry Failed ({_lastFailedPanelIds.Count})";
                RetryFailedButton.Visibility = Visibility.Visible;
            }
            else
            {
                RetryFailedButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void OnRetryFailedClicked(object sender, RoutedEventArgs e)
        {
            if (_lastPrompt is null || _lastFailedPanelIds.Count == 0) return;

            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            var targets = ws.Panels.Where(p => _lastFailedPanelIds.Contains(p.Id)).ToList();
            if (targets.Count == 0) return;

            await DispatchBroadcastAsync(_lastPrompt, targets, isRetry: true);
        }

        // ─────────────────────────────────────────────
        //  Presets
        // ─────────────────────────────────────────────

        private void BuildPresetsMenu()
        {
            PresetsMenu.Items.Clear();

            foreach (var preset in _presets.Presets)
            {
                var captured = preset;
                var item = new MenuFlyoutItem
                {
                    Text = captured.Name,
                    Icon = new FontIcon { Glyph = "\uE734" }
                };
                item.Click += (s, e) => ApplyPreset(captured);
                PresetsMenu.Items.Add(item);
            }

            if (_presets.Presets.Count > 0)
                PresetsMenu.Items.Add(new MenuFlyoutSeparator());

            var saveItem = new MenuFlyoutItem
            {
                Text = "Save current targets as...",
                Icon = new FontIcon { Glyph = "\uE710" }
            };
            saveItem.Click += async (s, e) => await SaveCurrentAsPresetAsync();
            PresetsMenu.Items.Add(saveItem);
        }

        private async Task SaveCurrentAsPresetAsync()
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            var selected = ws.Panels.Where(p => p.IsPromptTarget && !string.IsNullOrEmpty(p.AccountId)).ToList();
            if (selected.Count == 0)
            {
                _notifications.Show("Nothing to Save",
                    "Select at least one target first.", NotificationSeverity.Warning);
                return;
            }

            var nameBox = new TextBox
            {
                PlaceholderText = "e.g. Coding Team, Research, Quick Compare",
                MaxLength = 40
            };

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Save Preset",
                Content = nameBox,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var name = string.IsNullOrWhiteSpace(nameBox.Text) ? "Preset" : nameBox.Text.Trim();
            _presets.CreatePreset(name,
                selected.Select(p => new TargetRef { ServiceId = p.ServiceId, AccountId = p.AccountId }));

            _notifications.Show("Preset Saved", name, NotificationSeverity.Success);
            BuildPresetsMenu();
        }

        private void ApplyPreset(TargetPreset preset)
        {
            var ws = _manager.ActiveWorkspace;
            if (ws is null) return;

            foreach (var panel in ws.Panels)
            {
                var matches = preset.Targets.Any(t =>
                    t.ServiceId == panel.ServiceId && t.AccountId == panel.AccountId);

                panel.IsPromptTarget = matches && !string.IsNullOrEmpty(panel.AccountId);
            }

            _ = _manager.SaveAsync();
            RefreshTargetChips();
            UpdateComposerStatus();
            RefreshAllPanelCheckboxes();

            _notifications.Show("Preset Applied", preset.Name, NotificationSeverity.Information);
        }

        // ─────────────────────────────────────────────
        //  History
        // ─────────────────────────────────────────────

        private void RefreshHistoryList()
        {
            HistoryList.Children.Clear();

            var entries = _presets.History;
            if (entries.Count == 0)
            {
                HistoryList.Children.Add(new TextBlock
                {
                    Text = "No prompts sent yet.",
                    Opacity = 0.6,
                    FontSize = 12
                });
                ClearHistoryButton.Visibility = Visibility.Collapsed;
                return;
            }

            ClearHistoryButton.Visibility = Visibility.Visible;

            foreach (var entry in entries)
            {
                var captured = entry;

                var card = new Border
                {
                    Background = (Brush)Application.Current.Resources["AppSecondaryBrush"],
                    BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 8, 10, 8)
                };

                var stack = new StackPanel { Spacing = 4 };
                var preview = captured.Text.Length > 120
                    ? captured.Text.Substring(0, 120) + "..."
                    : captured.Text;

                stack.Children.Add(new TextBlock
                {
                    Text = preview,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.Resources["AppTextPrimaryBrush"]
                });

                var targetLabels = string.Join(", ", captured.TargetLabels.Take(3));
                if (captured.TargetLabels.Count > 3) targetLabels += " +" + (captured.TargetLabels.Count - 3);

                stack.Children.Add(new TextBlock
                {
                    Text = $"{captured.Timestamp:HH:mm} · {targetLabels}",
                    FontSize = 10,
                    Opacity = 0.55,
                    TextWrapping = TextWrapping.Wrap
                });

                card.Child = stack;

                var btn = new Button
                {
                    Content = card,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    CornerRadius = new CornerRadius(8)
                };

                btn.Click += (s, e) =>
                {
                    PromptInput.Text = captured.Text;
                    HistoryFlyout.Hide();
                };

                HistoryList.Children.Add(btn);
            }
        }

        private void OnClearHistoryClicked(object sender, RoutedEventArgs e)
        {
            _presets.ClearHistory();
            RefreshHistoryList();
        }

        // ─────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────

        private string GetPanelLabel(MultiAIPanel panel)
        {
            var svc = _serviceManager.FindService(panel.ServiceId);
            var acc = svc?.Accounts.FirstOrDefault(a => a.Id == panel.AccountId);

            if (svc is null || acc is null) return "Empty";
            return $"{svc.Name} · {acc.Name}";
        }
    }
}