using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DIHub.APP.Controls;
using DIHub.APP.ViewModels;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.System;

namespace DIHub.APP.Views
{
    public sealed partial class MainView : UserControl
    {
        public MainWindowViewModel ViewModel { get; }

        private readonly Dictionary<string, WebView2Host> _webViewHosts = new();
        private readonly Dictionary<string, TabItem> _accountIdToTab = new();
        private readonly Stack<(string Title, string Url, string Icon, string? ServiceId, string? AccountId, string? AccountName)> _closedTabs = new();
        private readonly IBrowserService _browserService;
        private readonly IAIServiceManager _serviceManager;
        private readonly IAIAccountManager _accountManager;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly IMultiAIWorkspaceManager _multiAIWorkspaceManager;
        private readonly INotificationService _notifications;
        private readonly ISettingsService _settings;

        private const double SidebarExpandedWidth = 240;
        private const double SidebarCollapsedWidth = 0;
        private const int SidebarAnimationMs = 200;

        public event EventHandler<FrameworkElement>? TitleBarReady;

        public MainView()
        {
            InitializeComponent();
            ViewModel = App.GetService<MainWindowViewModel>();
            _browserService = App.GetService<IBrowserService>();
            _serviceManager = App.GetService<IAIServiceManager>();
            _accountManager = App.GetService<IAIAccountManager>();
            _workspaceManager = App.GetService<IWorkspaceManager>();
            _multiAIWorkspaceManager = App.GetService<IMultiAIWorkspaceManager>();
            _notifications = App.GetService<INotificationService>();
            _settings = App.GetService<ISettingsService>();

            ViewModel.Tabs.CollectionChanged += OnTabsCollectionChanged;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            _serviceManager.ServicesChanged += (s, e) => DispatcherQueue.TryEnqueue(UpdateSidebarEmptyState);

            _workspaceManager.WorkspacesChanged += (s, e) => DispatcherQueue.TryEnqueue(BuildWorkspaceMenu);
            _workspaceManager.ActiveWorkspaceChanged += OnActiveWorkspaceChanged;

            SidebarPanel.Width = ViewModel.IsSidebarExpanded ? SidebarExpandedWidth : SidebarCollapsedWidth;
            UpdateToolbarForActiveTab();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            TitleBarReady?.Invoke(this, AppTitleBar);
            BuildWorkspaceMenu();
            UpdateSidebarEmptyState();
            TryRestorePreviousSession();
        }

        private void UpdateSidebarEmptyState()
        {
            var isEmpty = _serviceManager.Services.Count == 0;
            SidebarEmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────
        //  Search Box → Command Palette
        // ─────────────────────────────────────────────

        private void OnSearchBoxClicked(object sender, RoutedEventArgs e)
        {
            ShowCommandPalette();
        }

        // ─────────────────────────────────────────────
        //  Session Restore
        // ─────────────────────────────────────────────

        private void TryRestorePreviousSession()
        {
            try
            {
                if (!_settings.Current.RestorePreviousSession) return;
                if (_settings.Current.LastSessionTabUrls.Count == 0) return;

                var urls = _settings.Current.LastSessionTabUrls;
                var serviceIds = _settings.Current.LastSessionTabServiceIds;
                var accountIds = _settings.Current.LastSessionTabAccountIds;

                for (int i = 0; i < urls.Count; i++)
                {
                    var url = urls[i];
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    AIAccount? acc = null;
                    if (i < accountIds.Count && !string.IsNullOrEmpty(accountIds[i]))
                        acc = _accountManager.GetAccount(accountIds[i]);

                    if (acc is null && i < serviceIds.Count && !string.IsNullOrEmpty(serviceIds[i]))
                    {
                        var svc = _serviceManager.FindService(serviceIds[i]);
                        acc = svc?.Accounts.FirstOrDefault(a => a.IsDefault) ?? svc?.Accounts.FirstOrDefault();
                    }

                    if (acc is not null)
                    {
                        OpenAccount(acc, forceNewTab: true);
                        continue;
                    }

                    var tab = new TabItem
                    {
                        Title = ShortName(url),
                        Url = url,
                        Icon = "\uE774",
                        IsActive = false
                    };

                    ViewModel.Tabs.Add(tab);
                }

                var activeIdx = _settings.Current.LastSessionActiveIndex;
                if (activeIdx >= 0 && activeIdx < ViewModel.Tabs.Count)
                {
                    ViewModel.ActivateTab(ViewModel.Tabs[activeIdx]);
                }
                else if (ViewModel.Tabs.Count > 0)
                {
                    ViewModel.ActivateTab(ViewModel.Tabs[0]);
                }
            }
            catch { }
        }

        public void CaptureSession()
        {
            try
            {
                var urls = new List<string>();
                var serviceIds = new List<string>();
                var accountIds = new List<string>();

                foreach (var tab in ViewModel.Tabs)
                {
                    urls.Add(tab.Url ?? string.Empty);
                    serviceIds.Add(tab.AIServiceId ?? string.Empty);
                    accountIds.Add(tab.AccountId ?? string.Empty);
                }

                var activeIdx = ViewModel.ActiveTab is not null
                    ? ViewModel.Tabs.IndexOf(ViewModel.ActiveTab)
                    : -1;

                _settings.Update(s =>
                {
                    s.LastSessionTabUrls = urls;
                    s.LastSessionTabServiceIds = serviceIds;
                    s.LastSessionTabAccountIds = accountIds;
                    s.LastSessionActiveIndex = activeIdx;
                });
            }
            catch { }
        }

        private static string ShortName(string url)
        {
            try
            {
                var host = new Uri(url).Host;
                return host.StartsWith("www.") ? host.Substring(4) : host;
            }
            catch { return url; }
        }

        // ─────────────────────────────────────────────
        //  Multi-AI
        // ─────────────────────────────────────────────

        private void OnMultiAIButtonClicked(object sender, RoutedEventArgs e)
        {
            try { MultiAIOverlay.Open(); }
            catch (Exception ex)
            {
                _notifications.Show("Multi-AI Error", ex.Message, NotificationSeverity.Error);
            }
        }

        private void OnMultiAIInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            try { MultiAIOverlay.Open(); }
            catch (Exception ex)
            {
                _notifications.Show("Multi-AI Error", ex.Message, NotificationSeverity.Error);
            }
        }

        private void OnMultiAIRequestClose(object? sender, EventArgs e) { }

        // ─────────────────────────────────────────────
        //  Add new AI Service
        // ─────────────────────────────────────────────

        private async void OnAddNewServiceClicked(object sender, RoutedEventArgs e)
        {
            var content = new AddEditServiceDialog();
            content.LoadService(null);

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Add AI Service",
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = content
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                if (!content.Validate(out var error))
                {
                    content.ShowError(error);
                    args.Cancel = true;
                }
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var newSvc = content.BuildService();
            _serviceManager.AddService(newSvc);

            _notifications.Show("AI Service Added", newSvc.Name, NotificationSeverity.Success);
        }

        // ─────────────────────────────────────────────
        //  Service menu handlers
        // ─────────────────────────────────────────────

        private void OnServiceOpenDefaultClicked(object sender, RoutedEventArgs e)
        {
            if (TryGetServiceFromMenu(sender, out var svc))
            {
                var acc = svc!.Accounts.FirstOrDefault(a => a.IsDefault)
                          ?? svc.Accounts.FirstOrDefault();
                if (acc is not null) OpenAccount(acc);
            }
        }

        private void OnServiceOpenExternalClicked(object sender, RoutedEventArgs e)
        {
            if (TryGetServiceFromMenu(sender, out var svc))
                _browserService.OpenInDefaultBrowser(svc!.Url);
        }

        private async void OnDeleteServiceClicked(object sender, RoutedEventArgs e)
        {
            if (!TryGetServiceFromMenu(sender, out var svc)) return;

            var accountCount = svc!.Accounts.Count;

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = $"Delete \"{svc.Name}\"?",
                Content = $"This will remove the service, its {accountCount} account(s), all their browser profiles, " +
                          "and any open tabs. This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var tabsToClose = ViewModel.Tabs
                .Where(t => t.AIServiceId == svc.Id)
                .ToList();

            foreach (var tab in tabsToClose)
            {
                if (tab.AccountId is not null)
                    _accountIdToTab.Remove(tab.AccountId);

                ViewModel.CloseTab(tab);
            }

            try { _multiAIWorkspaceManager.RemovePanelsForService(svc.Id); } catch { }

            try { await _accountManager.DeleteAllProfilesForServiceAsync(svc.Id); } catch { }

            _serviceManager.RemoveService(svc.Id);

            _notifications.Show("AI Service Removed", svc.Name, NotificationSeverity.Information);
        }

        private static bool TryGetServiceFromMenu(object sender, out AIService? service)
        {
            service = null;
            if (sender is MenuFlyoutItem item)
            {
                if (item.Tag is AIService a) { service = a; return true; }
                if (item.DataContext is AIService dc) { service = dc; return true; }
            }
            return false;
        }

        // ─────────────────────────────────────────────
        //  Account actions
        // ─────────────────────────────────────────────

        private void OnAccountClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AIAccount acc)
                OpenAccount(acc);
        }

        private void OnAccountOpenClicked(object sender, RoutedEventArgs e)
        {
            if (TryGetAccountFromMenu(sender, out var acc)) OpenAccount(acc!);
        }

        private void OnAccountOpenNewTabClicked(object sender, RoutedEventArgs e)
        {
            if (TryGetAccountFromMenu(sender, out var acc)) OpenAccount(acc!, forceNewTab: true);
        }

        private void OnAccountSetDefaultClicked(object sender, RoutedEventArgs e)
        {
            if (TryGetAccountFromMenu(sender, out var acc))
            {
                _accountManager.SetDefaultAccount(acc!.Id);
                _notifications.Show("Default Set", $"{acc.Name} is now the default account.", NotificationSeverity.Success);
            }
        }

        private void OnAccountFavoriteClicked(object sender, RoutedEventArgs e)
        {
            if (TryGetAccountFromMenu(sender, out var acc))
                _accountManager.ToggleFavorite(acc!.Id);
        }

        private async void OnAccountRenameClicked(object sender, RoutedEventArgs e)
        {
            if (!TryGetAccountFromMenu(sender, out var acc)) return;

            var nameBox = new TextBox { Text = acc!.Name, MaxLength = 40 };
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Rename Account",
                Content = nameBox,
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            _accountManager.RenameAccount(acc.Id, nameBox.Text);

            if (_accountIdToTab.TryGetValue(acc.Id, out var tab))
                tab.AccountName = nameBox.Text;
        }

        private async void OnAccountClearSessionClicked(object sender, RoutedEventArgs e)
        {
            if (!TryGetAccountFromMenu(sender, out var acc)) return;

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Clear Session",
                Content = $"Clear cookies, cache and login for \"{acc!.Name}\"? Other accounts of this service are not affected.",
                PrimaryButtonText = "Clear",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            await _accountManager.ClearProfileAsync(acc.Id);
            _notifications.Show("Session Cleared", acc.Name, NotificationSeverity.Success);
        }

        private void OnAccountOpenExternalClicked(object sender, RoutedEventArgs e)
        {
            if (TryGetAccountFromMenu(sender, out var acc))
            {
                var svc = _serviceManager.FindService(acc!.ServiceId);
                if (svc is not null)
                    _browserService.OpenInDefaultBrowser(svc.Url);
            }
        }

        private async void OnAccountDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (!TryGetAccountFromMenu(sender, out var acc)) return;

            var svc = _serviceManager.FindService(acc!.ServiceId);
            if (svc is null) return;

            if (svc.Accounts.Count <= 1)
            {
                _notifications.Show("Cannot Delete", "The last account of a service cannot be deleted.", NotificationSeverity.Warning);
                return;
            }

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Delete Account",
                Content = $"Delete \"{acc.Name}\" and remove its browser profile? This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            if (_accountIdToTab.TryGetValue(acc.Id, out var tab))
            {
                _accountIdToTab.Remove(acc.Id);
                ViewModel.CloseTab(tab);
            }

            await _accountManager.DeleteAccountAsync(acc.Id);
            _notifications.Show("Account Deleted", acc.Name, NotificationSeverity.Information);
        }

        private async void OnAddAccountClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not AIService svc) return;

            var nameBox = new TextBox
            {
                PlaceholderText = "e.g. Personal, Work, University",
                MaxLength = 40
            };

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = $"Add Account to {svc.Name}",
                Content = nameBox,
                PrimaryButtonText = "Create & Open",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var name = string.IsNullOrWhiteSpace(nameBox.Text) ? "New Account" : nameBox.Text.Trim();
            var acc = _accountManager.AddAccount(svc.Id, name);

            _notifications.Show("Account Added", $"{svc.Name} / {acc.Name}", NotificationSeverity.Success);
            OpenAccount(acc);
        }

        private static bool TryGetAccountFromMenu(object sender, out AIAccount? account)
        {
            account = null;
            if (sender is not MenuFlyoutItem item) return false;
            if (item.Tag is AIAccount a) { account = a; return true; }
            if (item.DataContext is AIAccount dc) { account = dc; return true; }
            return false;
        }

        // ─────────────────────────────────────────────
        //  Open account
        // ─────────────────────────────────────────────

        private void OpenAccount(AIAccount account, bool forceNewTab = false)
        {
            var svc = _serviceManager.FindService(account.ServiceId);
            if (svc is null) return;

            if (!forceNewTab &&
                _accountIdToTab.TryGetValue(account.Id, out var existing) &&
                ViewModel.Tabs.Contains(existing))
            {
                ViewModel.ActivateTab(existing);
                _accountManager.MarkUsed(account.Id);
                return;
            }

            var tab = new TabItem
            {
                Title = svc.Name,
                AccountName = account.Name,
                Url = svc.Url,
                Icon = account.Icon,
                AIServiceId = svc.Id,
                AccountId = account.Id,
                IsActive = true
            };

            if (ViewModel.ActiveTab is not null)
                ViewModel.ActiveTab.IsActive = false;

            ViewModel.Tabs.Add(tab);
            ViewModel.ActiveTab = tab;

            _accountIdToTab[account.Id] = tab;
            _accountManager.MarkUsed(account.Id);
        }

        // ─────────────────────────────────────────────
        //  Tab lifecycle
        // ─────────────────────────────────────────────

        private void OnTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
            {
                foreach (TabItem tab in e.NewItems)
                {
                    var host = new WebView2Host { Visibility = Visibility.Collapsed };

                    if (tab.AccountId is not null)
                    {
                        try
                        {
                            var profilePath = _accountManager.GetProfilePath(tab.AccountId);
                            host.SetProfilePath(profilePath);
                        }
                        catch { }
                    }

                    var capturedTab = tab;

                    host.NavigationCompleted += (s, url) =>
                    {
                        if (!string.IsNullOrEmpty(url)) capturedTab.Url = url;
                    };

                    host.TitleChanged += (s, title) =>
                    {
                        if (!string.IsNullOrWhiteSpace(title) && capturedTab.AccountId is null)
                            capturedTab.Title = title;
                    };

                    host.NavigationStateChanged += (s, args) =>
                    {
                        if (ViewModel.ActiveTab != capturedTab) return;
                        ApplyNavigationState(args);
                    };

                    ContentHost.Children.Add(host);
                    _webViewHosts[tab.Id] = host;
                    host.Navigate(tab.Url);
                }
            }

            if (e.OldItems is not null)
            {
                foreach (TabItem tab in e.OldItems)
                {
                    if (_webViewHosts.TryGetValue(tab.Id, out var host))
                    {
                        host.Dispose();
                        ContentHost.Children.Remove(host);
                        _webViewHosts.Remove(tab.Id);
                    }
                }
            }

            UpdateVisibility();
        }

        // ─────────────────────────────────────────────
        //  Keyboard shortcuts
        // ─────────────────────────────────────────────

        private void OnFocusAddressBarInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            AddressBar.Focus(FocusState.Programmatic);
            AddressBar.SelectAll();
        }

        private void OnNewTabInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var first = _serviceManager.Services.SelectMany(s => s.Accounts).FirstOrDefault(a => a.Enabled);
            if (first is not null) OpenAccount(first);
            else _notifications.Show("No Account", "Add an account first.", NotificationSeverity.Warning);
        }

        private void OnCloseTabInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            if (ViewModel.ActiveTab is { } tab) CloseTabWithHistory(tab);
        }

        private void OnRestoreTabInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            if (_closedTabs.Count == 0)
            {
                _notifications.Show("Nothing to Restore", "No recently closed tab.", NotificationSeverity.Information);
                return;
            }

            var (title, url, icon, sid, aid, aname) = _closedTabs.Pop();
            var newTab = new TabItem
            {
                Title = title,
                Url = url,
                Icon = icon,
                AIServiceId = sid,
                AccountId = aid,
                AccountName = aname,
                IsActive = true
            };
            if (ViewModel.ActiveTab is not null) ViewModel.ActiveTab.IsActive = false;
            ViewModel.Tabs.Add(newTab);
            ViewModel.ActiveTab = newTab;
            if (aid is not null) _accountIdToTab[aid] = newTab;
        }

        private void OnReloadInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        { args.Handled = true; GetActiveHost()?.Reload(); }

        private void OnNextTabInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        { args.Handled = true; CycleTab(+1); }

        private void OnPrevTabInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        { args.Handled = true; CycleTab(-1); }

        private void OnDevToolsInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        { args.Handled = true; GetActiveHost()?.OpenDevTools(); }

        private void CycleTab(int delta)
        {
            if (ViewModel.Tabs.Count <= 1) return;
            var idx = ViewModel.Tabs.IndexOf(ViewModel.ActiveTab!);
            if (idx < 0) idx = 0;
            var next = ((idx + delta) % ViewModel.Tabs.Count + ViewModel.Tabs.Count) % ViewModel.Tabs.Count;
            ViewModel.ActivateTab(ViewModel.Tabs[next]);
        }

        // ─────────────────────────────────────────────
        //  Workspaces
        // ─────────────────────────────────────────────

        private void BuildWorkspaceMenu()
        {
            WorkspaceMenu.Items.Clear();

            foreach (var ws in _workspaceManager.Workspaces)
            {
                var item = new MenuFlyoutItem
                {
                    Text = ws.Name,
                    Tag = ws.Id,
                    Icon = new FontIcon { Glyph = ws.Icon }
                };
                if (_workspaceManager.ActiveWorkspace?.Id == ws.Id)
                    item.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                item.Click += (s, e) =>
                {
                    if (s is MenuFlyoutItem mfi && mfi.Tag is string id)
                        SwitchToWorkspace(id);
                };
                WorkspaceMenu.Items.Add(item);
            }

            WorkspaceMenu.Items.Add(new MenuFlyoutSeparator());

            var newItem = new MenuFlyoutItem { Text = "New Workspace...", Icon = new FontIcon { Glyph = "\uE710" } };
            newItem.Click += async (s, e) => await CreateNewWorkspaceAsync();
            WorkspaceMenu.Items.Add(newItem);

            if (_workspaceManager.ActiveWorkspace is { } active && _workspaceManager.Workspaces.Count > 1)
            {
                var deleteItem = new MenuFlyoutItem { Text = $"Delete \"{active.Name}\"", Icon = new FontIcon { Glyph = "\uE74D" } };
                deleteItem.Click += async (s, e) => await DeleteActiveWorkspaceAsync();
                WorkspaceMenu.Items.Add(deleteItem);
            }
        }

        private async Task CreateNewWorkspaceAsync()
        {
            var nameBox = new TextBox { PlaceholderText = "Workspace name", MaxLength = 40 };
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "New Workspace",
                Content = nameBox,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;
            var name = string.IsNullOrWhiteSpace(nameBox.Text) ? "New Workspace" : nameBox.Text.Trim();
            var ws = _workspaceManager.CreateWorkspace(name);
            SwitchToWorkspace(ws.Id);
        }

        private async Task DeleteActiveWorkspaceAsync()
        {
            var active = _workspaceManager.ActiveWorkspace;
            if (active is null) return;

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Delete Workspace",
                Content = $"Delete \"{active.Name}\"? Open tabs will be lost.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;
            _workspaceManager.DeleteWorkspace(active.Id);
        }

        private void SwitchToWorkspace(string id)
        {
            if (_workspaceManager.ActiveWorkspace?.Id == id) return;
            CaptureCurrentTabsIntoActiveWorkspace();
            ViewModel.ClearTabs();
            _accountIdToTab.Clear();
            _workspaceManager.SwitchWorkspace(id);
            RestoreTabsFromActiveWorkspace();
        }

        private void CaptureCurrentTabsIntoActiveWorkspace()
        {
            var urls = ViewModel.Tabs.Select(t => t.Url).ToList();
            _workspaceManager.CaptureCurrentTabs(urls, ViewModel.ActiveTab?.Url);
        }

        private void RestoreTabsFromActiveWorkspace()
        {
            var ws = _workspaceManager.ActiveWorkspace;
            if (ws is null) return;

            foreach (var url in ws.OpenTabs)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;

                var svc = _serviceManager.Services.FirstOrDefault(s => s.Url == url);
                if (svc is not null)
                {
                    var acc = svc.Accounts.FirstOrDefault(a => a.IsDefault) ?? svc.Accounts.FirstOrDefault();
                    if (acc is not null) { OpenAccount(acc); continue; }
                }

                var tab = new TabItem { Title = ShortName(url), Url = url, Icon = "\uE774", IsActive = true };
                if (ViewModel.ActiveTab is not null) ViewModel.ActiveTab.IsActive = false;
                ViewModel.Tabs.Add(tab);
                ViewModel.ActiveTab = tab;
            }
        }

        private void OnActiveWorkspaceChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ViewModel.ActiveWorkspace = _workspaceManager.ActiveWorkspace;
                BuildWorkspaceMenu();
            });
        }

        // ─────────────────────────────────────────────
        //  Title Bar buttons
        // ─────────────────────────────────────────────

        private void OnSidebarToggleClicked(object sender, RoutedEventArgs e)
            => ViewModel.ToggleSidebarCommand.Execute(null);

        private void OnSettingsButtonClicked(object sender, RoutedEventArgs e) => SettingsOverlay.Open();
        private void OnSettingsRequestClose(object? sender, EventArgs e) => SettingsOverlay.Close();
        private void OnSettingsClearAllData(object? sender, EventArgs e) { }

        // ─────────────────────────────────────────────
        //  Command Palette
        // ─────────────────────────────────────────────

        private void OnCommandPaletteInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        { args.Handled = true; ShowCommandPalette(); }

        private void ShowCommandPalette()
        {
            CommandPaletteOverlay.SetCommands(BuildCommands());
            CommandPaletteOverlay.Open();
        }

        private void OnCommandPaletteRequestClose(object? sender, EventArgs e)
            => CommandPaletteOverlay.Close();

        private List<CommandItem> BuildCommands()
        {
            var cmds = new List<CommandItem>();

            foreach (var svc in _serviceManager.Services)
            {
                foreach (var acc in svc.Accounts)
                {
                    var capturedSvc = svc;
                    var capturedAcc = acc;
                    cmds.Add(new CommandItem
                    {
                        Title = $"Open {capturedSvc.Name} / {capturedAcc.Name}",
                        Subtitle = capturedSvc.Url,
                        Icon = capturedAcc.Icon,
                        CategoryLabel = "AI Account",
                        Category = CommandCategory.AIService,
                        Keywords = $"{capturedSvc.Name} {capturedAcc.Name}",
                        Execute = () => OpenAccount(capturedAcc)
                    });
                }
            }

            cmds.Add(new CommandItem
            {
                Title = "Open Multi-AI Workspace",
                Subtitle = "Open 2-4 AIs side by side",
                Icon = "\uE71D",
                CategoryLabel = "Multi-AI",
                Category = CommandCategory.Appearance,
                Keywords = "multi split grid compare",
                Execute = () => MultiAIOverlay.Open()
            });

            cmds.Add(new CommandItem
            {
                Title = "Add AI Service...",
                Subtitle = "Register a brand new AI service",
                Icon = "\uE710",
                CategoryLabel = "Services",
                Category = CommandCategory.AIService,
                Keywords = "new service add ai",
                Execute = () => OnAddNewServiceClicked(this, new RoutedEventArgs())
            });

            cmds.Add(new CommandItem
            {
                Title = "Add Account...",
                Subtitle = "Create a new account for a service",
                Icon = "\uE77B",
                CategoryLabel = "Services",
                Category = CommandCategory.AIService,
                Keywords = "new account",
                Execute = () =>
                {
                    var firstSvc = _serviceManager.Services.FirstOrDefault();
                    if (firstSvc is not null)
                        OnAddAccountClicked(new Button { Tag = firstSvc }, new RoutedEventArgs());
                }
            });

            cmds.Add(new CommandItem
            {
                Title = "Restore Default AI Services",
                Subtitle = "Reset to the original list of 12 AI services",
                Icon = "\uE777",
                CategoryLabel = "Services",
                Category = CommandCategory.AIService,
                Keywords = "reset restore defaults services ai",
                Execute = async () =>
                {
                    var dialog = new ContentDialog
                    {
                        XamlRoot = RootGrid.XamlRoot,
                        Title = "Restore Default AI Services?",
                        Content = "This will reset the AI services list to the factory defaults: " +
                                  "7 popular (ChatGPT, Gemini, Claude, Perplexity, Grok, DeepSeek, Copilot) + " +
                                  "5 free / multi-model (Poe, Mistral, OpenRouter, Google AI Studio, Groq).\n\n" +
                                  "Your custom services and their accounts will be removed. " +
                                  "Browser profiles on disk are NOT deleted.",
                        PrimaryButtonText = "Restore",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close
                    };

                    var result = await dialog.ShowAsync();
                    if (result != ContentDialogResult.Primary) return;

                    _serviceManager.ResetToDefaults();

                    _notifications.Show("Services Restored",
                        "The default AI services list has been restored.",
                        NotificationSeverity.Success);
                }
            });

            cmds.Add(new CommandItem
            {
                Title = "Close Current Tab",
                Subtitle = "Close the active tab",
                Icon = "\uE711",
                CategoryLabel = "Tabs",
                Category = CommandCategory.Navigation,
                Keywords = "close",
                Execute = () =>
                {
                    if (ViewModel.ActiveTab is not null)
                        CloseTabWithHistory(ViewModel.ActiveTab);
                }
            });

            cmds.Add(new CommandItem
            {
                Title = "Restore Closed Tab",
                Subtitle = "Reopen the last closed tab",
                Icon = "\uE7A7",
                CategoryLabel = "Tabs",
                Category = CommandCategory.Navigation,
                Keywords = "restore reopen undo",
                Execute = () =>
                {
                    if (_closedTabs.Count > 0)
                    {
                        var (title, url, icon, sid, aid, aname) = _closedTabs.Pop();
                        var newTab = new TabItem
                        {
                            Title = title,
                            Url = url,
                            Icon = icon,
                            AIServiceId = sid,
                            AccountId = aid,
                            AccountName = aname,
                            IsActive = true
                        };
                        if (ViewModel.ActiveTab is not null) ViewModel.ActiveTab.IsActive = false;
                        ViewModel.Tabs.Add(newTab);
                        ViewModel.ActiveTab = newTab;
                        if (aid is not null) _accountIdToTab[aid] = newTab;
                    }
                }
            });

            cmds.Add(new CommandItem
            {
                Title = "Focus Address Bar",
                Subtitle = "Jump to the URL bar",
                Icon = "\uE774",
                CategoryLabel = "Navigation",
                Category = CommandCategory.Navigation,
                Keywords = "url address",
                Execute = () => AddressBar.Focus(FocusState.Programmatic)
            });

            cmds.Add(new CommandItem
            {
                Title = "Reload Current Page",
                Subtitle = "Refresh the active tab",
                Icon = "\uE72C",
                CategoryLabel = "Navigation",
                Category = CommandCategory.Navigation,
                Keywords = "refresh",
                Execute = () => GetActiveHost()?.Reload()
            });

            cmds.Add(new CommandItem
            {
                Title = "Open in External Browser",
                Subtitle = "Open current page in the default browser",
                Icon = "\uE8A7",
                CategoryLabel = "Navigation",
                Category = CommandCategory.Navigation,
                Execute = () =>
                {
                    var url = GetActiveHost()?.CurrentUrl;
                    if (!string.IsNullOrEmpty(url))
                        _browserService.OpenInDefaultBrowser(url);
                }
            });

            cmds.Add(new CommandItem
            {
                Title = "Toggle Sidebar",
                Subtitle = "Show or hide the sidebar",
                Icon = "\uE700",
                CategoryLabel = "Appearance",
                Category = CommandCategory.Appearance,
                Execute = () => ViewModel.ToggleSidebarCommand.Execute(null)
            });

            cmds.Add(new CommandItem
            {
                Title = "Open Settings",
                Subtitle = "Configure DI Hub",
                Icon = "\uE713",
                CategoryLabel = "Settings",
                Category = CommandCategory.Appearance,
                Keywords = "preferences options config",
                Execute = () => SettingsOverlay.Open()
            });

            foreach (var ws in _workspaceManager.Workspaces)
            {
                var captured = ws;
                cmds.Add(new CommandItem
                {
                    Title = $"Switch to {captured.Name}",
                    Subtitle = "Workspace",
                    Icon = captured.Icon,
                    CategoryLabel = "Workspace",
                    Category = CommandCategory.Workspace,
                    Keywords = captured.Name + " workspace",
                    Execute = () => SwitchToWorkspace(captured.Id)
                });
            }

            cmds.Add(new CommandItem
            {
                Title = "New Workspace...",
                Subtitle = "Create a new workspace",
                Icon = "\uE710",
                CategoryLabel = "Workspace",
                Category = CommandCategory.Workspace,
                Keywords = "workspace create",
                Execute = () => _ = CreateNewWorkspaceAsync()
            });

            cmds.Add(new CommandItem
            {
                Title = "Open Developer Tools",
                Subtitle = "DevTools for the active tab",
                Icon = "\uEC7A",
                CategoryLabel = "Developer",
                Category = CommandCategory.Developer,
                Keywords = "devtools console inspect",
                Execute = () => GetActiveHost()?.OpenDevTools()
            });

            return cmds;
        }

        // ─────────────────────────────────────────────
        //  Sidebar animation
        // ─────────────────────────────────────────────

        private void AnimateSidebarTo(double targetWidth)
        {
            var animation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = new Duration(TimeSpan.FromMilliseconds(SidebarAnimationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(animation, SidebarPanel);
            Storyboard.SetTargetProperty(animation, "Width");
            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void OnSidebarSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SidebarPanel.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
            };
        }

        // ─────────────────────────────────────────────
        //  Tab bar
        // ─────────────────────────────────────────────

        private void OnTabActivated(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TabItem tab)
                ViewModel.ActivateTab(tab);
        }

        private void OnTabCloseClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not TabItem tab) return;
            CloseTabWithHistory(tab);
        }

        private void CloseTabWithHistory(TabItem tab)
        {
            _closedTabs.Push((tab.Title, tab.Url, tab.Icon, tab.AIServiceId, tab.AccountId, tab.AccountName));

            if (tab.AccountId is not null &&
                _accountIdToTab.TryGetValue(tab.AccountId, out var mapped) && mapped == tab)
            {
                _accountIdToTab.Remove(tab.AccountId);
            }

            ViewModel.CloseTab(tab);
        }

        // ─────────────────────────────────────────────
        //  Toolbar
        // ─────────────────────────────────────────────

        private void OnBackClicked(object sender, RoutedEventArgs e) => GetActiveHost()?.GoBack();
        private void OnForwardClicked(object sender, RoutedEventArgs e) => GetActiveHost()?.GoForward();
        private void OnReloadClicked(object sender, RoutedEventArgs e) => GetActiveHost()?.Reload();

        private void OnOpenExternalClicked(object sender, RoutedEventArgs e)
        {
            var url = GetActiveHost()?.CurrentUrl;
            if (!string.IsNullOrEmpty(url)) _browserService.OpenInDefaultBrowser(url);
        }

        private void OnAddressBarKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter) return;
            e.Handled = true;

            var host = GetActiveHost();
            if (host is null) return;

            var input = AddressBar.Text;
            if (string.IsNullOrWhiteSpace(input)) return;

            var target = _browserService.NormalizeUrlOrSearch(input);
            host.Navigate(target);
        }

        // ─────────────────────────────────────────────
        //  ViewModel events
        // ─────────────────────────────────────────────

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.ActiveTab):
                    UpdateVisibility();
                    UpdateToolbarForActiveTab();
                    break;

                case nameof(ViewModel.IsSidebarExpanded):
                    AnimateSidebarTo(ViewModel.IsSidebarExpanded
                        ? SidebarExpandedWidth
                        : SidebarCollapsedWidth);
                    break;
            }
        }

        private void UpdateVisibility()
        {
            var activeId = ViewModel.ActiveTab?.Id;

            foreach (var kvp in _webViewHosts)
            {
                kvp.Value.Visibility = (kvp.Key == activeId)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            WelcomeOverlay.Visibility = (ViewModel.ActiveTab is null)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private WebView2Host? GetActiveHost()
        {
            var active = ViewModel.ActiveTab;
            if (active is null) return null;
            return _webViewHosts.TryGetValue(active.Id, out var host) ? host : null;
        }

        private void UpdateToolbarForActiveTab()
        {
            var host = GetActiveHost();
            if (host is null)
            {
                BackButton.IsEnabled = false;
                ForwardButton.IsEnabled = false;
                ReloadButton.IsEnabled = false;
                OpenExternalButton.IsEnabled = false;
                AddressBar.Text = string.Empty;
                AddressBar.IsEnabled = false;
                return;
            }

            BackButton.IsEnabled = host.CanGoBack;
            ForwardButton.IsEnabled = host.CanGoForward;
            ReloadButton.IsEnabled = true;
            OpenExternalButton.IsEnabled = true;
            AddressBar.IsEnabled = true;

            if (AddressBar.FocusState == FocusState.Unfocused)
                AddressBar.Text = host.CurrentUrl ?? string.Empty;
        }

        private void ApplyNavigationState(NavigationStateChangedEventArgs args)
        {
            BackButton.IsEnabled = args.CanGoBack;
            ForwardButton.IsEnabled = args.CanGoForward;
            ReloadIcon.Glyph = args.IsLoading ? "\uE711" : "\uE72C";

            if (AddressBar.FocusState == FocusState.Unfocused)
                AddressBar.Text = args.Url;
        }
    }
}