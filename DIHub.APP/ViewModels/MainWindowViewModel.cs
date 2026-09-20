using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;

namespace DIHub.APP.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IThemeService _themeService;
        private readonly INotificationService _notificationService;
        private readonly IAIServiceManager _serviceManager;
        private readonly IWorkspaceManager _workspaceManager;

        public ObservableCollection<TabItem> Tabs { get; } = new();
        public ObservableCollection<AIService> Services { get; } = new();
        public ObservableCollection<Workspace> Workspaces { get; } = new();

        [ObservableProperty]
        private bool _isSidebarExpanded;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private TabItem? _activeTab;

        [ObservableProperty]
        private Workspace? _activeWorkspace;

        public MainWindowViewModel(
            IThemeService themeService,
            INotificationService notificationService,
            IAIServiceManager serviceManager,
            IWorkspaceManager workspaceManager)
        {
            _themeService = themeService;
            _notificationService = notificationService;
            _serviceManager = serviceManager;
            _workspaceManager = workspaceManager;

            IsSidebarExpanded = true;
            SearchText = string.Empty;

            _serviceManager.ServicesChanged += (s, e) => RefreshServices();
            _workspaceManager.WorkspacesChanged += (s, e) => RefreshWorkspaces();
            _workspaceManager.ActiveWorkspaceChanged += (s, e) => UpdateActiveWorkspace();

            RefreshServices();
            RefreshWorkspaces();
        }

        // ─────────────────────────────────────────────
        //  Services
        // ─────────────────────────────────────────────

        private void RefreshServices()
        {
            Services.Clear();
            foreach (var svc in _serviceManager.Services)
                Services.Add(svc);
        }

        // ─────────────────────────────────────────────
        //  Workspaces
        // ─────────────────────────────────────────────

        private void RefreshWorkspaces()
        {
            Workspaces.Clear();
            foreach (var ws in _workspaceManager.Workspaces)
                Workspaces.Add(ws);

            UpdateActiveWorkspace();
        }

        private void UpdateActiveWorkspace()
        {
            ActiveWorkspace = _workspaceManager.ActiveWorkspace;
        }

        // ─────────────────────────────────────────────
        //  Commands
        // ─────────────────────────────────────────────

        [RelayCommand]
        private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

        // ─────────────────────────────────────────────
        //  Tab management
        // ─────────────────────────────────────────────

        public TabItem AddTab(string title, string url, string icon, string? aiServiceId = null)
        {
            var tab = new TabItem
            {
                Title = title,
                Url = url,
                Icon = icon,
                AIServiceId = aiServiceId,
                IsActive = true
            };

            if (ActiveTab is not null)
                ActiveTab.IsActive = false;

            Tabs.Add(tab);
            ActiveTab = tab;
            return tab;
        }

        public void CloseTab(TabItem tab)
        {
            if (tab is null || !Tabs.Contains(tab)) return;

            var index = Tabs.IndexOf(tab);
            var wasActive = ActiveTab == tab;

            Tabs.Remove(tab);

            if (!wasActive) return;

            if (Tabs.Count == 0)
            {
                ActiveTab = null;
                return;
            }

            var newIndex = Math.Min(index, Tabs.Count - 1);
            ActiveTab = Tabs[newIndex];
            ActiveTab.IsActive = true;
        }

        public void ActivateTab(TabItem tab)
        {
            if (tab is null || !Tabs.Contains(tab)) return;
            if (ActiveTab == tab) return;

            if (ActiveTab is not null)
                ActiveTab.IsActive = false;

            ActiveTab = tab;
            tab.IsActive = true;
        }

        public void ClearTabs()
        {
            foreach (var tab in Tabs)
                tab.IsActive = false;

            Tabs.Clear();
            ActiveTab = null;
        }
    }
}