using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI.Dispatching;

namespace DIHub.APP.ViewModels
{
    public sealed partial class CatalogCardViewModel : ObservableObject
    {
        public ExtensionCatalogItem Item { get; }
        public bool HasDownloadUrl => !string.IsNullOrWhiteSpace(Item.DownloadUrl);

        [ObservableProperty]
        private bool _isInstalled;

        public CatalogCardViewModel(ExtensionCatalogItem item, bool isInstalled)
        {
            Item = item;
            _isInstalled = isInstalled;
        }
    }

    public sealed class InstalledItemViewModel
    {
        public ExtensionInfo Info { get; init; } = new();
        public ExtensionEffectiveState State { get; init; } = new();
    }

    public sealed partial class ExtensionsViewModel : ObservableObject
    {
        private readonly IExtensionManager _manager;
        private readonly IExtensionCatalogService _catalog;
        private readonly INotificationService _notifications;
        private readonly DispatcherQueue? _dispatcherQueue;

        private bool _initialized;

        public ObservableCollection<CatalogCardViewModel> FeaturedCards { get; } = new();
        public ObservableCollection<CatalogCardViewModel> AllCards { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();

        public ObservableCollection<InstalledItemViewModel> InstalledItems { get; } = new();

        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _selectedCategory = "All";

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _hasFeatured;
        [ObservableProperty] private bool _hasAnyResults = true;
        [ObservableProperty] private bool _hasInstalledExtensions;
        [ObservableProperty] private string _installedCountText = "No extensions installed";

        public ExtensionsViewModel(
            IExtensionManager manager,
            IExtensionCatalogService catalog,
            INotificationService notifications)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _manager.ExtensionsChanged += OnExtensionsChanged;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            IsLoading = true;
            try
            {
                await LoadCategoriesAsync();
                await ReloadCatalogAsync();
                RefreshInstalledList();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─────────────────────────────────────────────
        //  Catalog
        // ─────────────────────────────────────────────

        private async Task LoadCategoriesAsync()
        {
            var cats = await _catalog.GetCategoriesAsync();

            Categories.Clear();
            Categories.Add("All");
            foreach (var c in cats)
                Categories.Add(c);
        }

        private async Task ReloadCatalogAsync()
        {
            var query = SearchText ?? string.Empty;
            var category = SelectedCategory ?? "All";

            var results = string.IsNullOrWhiteSpace(query)
                ? await _catalog.GetAllAsync()
                : await _catalog.SearchAsync(query);

            if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
            {
                results = results
                    .Where(e => e.Categories.Any(c =>
                        string.Equals(c, category, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            var showFeatured = string.IsNullOrWhiteSpace(query)
                               && string.Equals(category, "All", StringComparison.OrdinalIgnoreCase);

            var featured = showFeatured
                ? results.Where(e => e.IsFeatured).ToList()
                : new List<ExtensionCatalogItem>();

            var all = results
                .OrderByDescending(e => e.IsFeatured)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            FeaturedCards.Clear();
            foreach (var item in featured)
                FeaturedCards.Add(new CatalogCardViewModel(item, IsInstalledByName(item.Name)));

            AllCards.Clear();
            foreach (var item in all)
                AllCards.Add(new CatalogCardViewModel(item, IsInstalledByName(item.Name)));

            HasFeatured = FeaturedCards.Count > 0;
            HasAnyResults = AllCards.Count > 0;
        }

        private bool IsInstalledByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return _manager.InstalledExtensions.Any(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        // ─────────────────────────────────────────────
        //  Installed
        // ─────────────────────────────────────────────

        private void RefreshInstalledList()
        {
            InstalledItems.Clear();

            foreach (var ext in _manager.InstalledExtensions
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            {
                InstalledItems.Add(new InstalledItemViewModel
                {
                    Info = ext,
                    State = GetSummaryState(ext.Id)
                });
            }

            var count = InstalledItems.Count;
            HasInstalledExtensions = count > 0;
            InstalledCountText = count switch
            {
                0 => "No extensions installed",
                1 => "1 extension installed",
                _ => $"{count} extensions installed"
            };
        }

        private ExtensionEffectiveState GetSummaryState(string extensionId)
        {
            var assignments = _manager.Assignments
                .Where(a => a.ExtensionId == extensionId)
                .ToList();

            var acc = assignments.FirstOrDefault(a => a.Scope == ExtensionScope.Account);
            if (acc is not null)
                return new ExtensionEffectiveState
                {
                    ExtensionId = extensionId,
                    IsEnabled = acc.IsEnabled,
                    Source = ExtensionScope.Account
                };

            var svc = assignments.FirstOrDefault(a => a.Scope == ExtensionScope.Service);
            if (svc is not null)
                return new ExtensionEffectiveState
                {
                    ExtensionId = extensionId,
                    IsEnabled = svc.IsEnabled,
                    Source = ExtensionScope.Service
                };

            var glob = assignments.FirstOrDefault(a => a.Scope == ExtensionScope.Global);
            if (glob is not null)
                return new ExtensionEffectiveState
                {
                    ExtensionId = extensionId,
                    IsEnabled = glob.IsEnabled,
                    Source = ExtensionScope.Global
                };

            return new ExtensionEffectiveState
            {
                ExtensionId = extensionId,
                IsEnabled = false,
                Source = ExtensionScope.None
            };
        }

        // ─────────────────────────────────────────────
        //  Install
        // ─────────────────────────────────────────────

        public Task<ExtensionInstallResult> InstallFromFolderAsync(string folder)
            => _manager.InstallFromFolderAsync(
                folder,
                ExtensionSourceType.LocalFolder,
                sourceUrl: null);

        public Task<ExtensionInstallResult> InstallFromZipAsync(string zipPath)
            => _manager.InstallFromZipAsync(zipPath);

        public Task<ExtensionInstallResult> InstallFromUrlAsync(
            string url,
            string? sha256,
            IProgress<DownloadProgress>? progress,
            CancellationToken ct = default)
            => _manager.InstallFromUrlAsync(url, sha256, progress, ct);

        // ─────────────────────────────────────────────
        //  Scope management
        // ─────────────────────────────────────────────

        /// <summary>Sets (or replaces) an assignment at the given scope.</summary>
        public async Task SetScopeAsync(
            string extensionId,
            ExtensionScope scope,
            string? serviceId,
            string? accountId,
            bool enabled)
        {
            await _manager.SetAssignmentAsync(
                extensionId,
                scope,
                serviceId,
                accountId,
                enabled);

            // ExtensionsChanged fires, UI refreshes.
        }

        /// <summary>Convenience: enable/disable at the Global scope.</summary>
        public async Task SetGlobalEnabledAsync(string extensionId, bool enabled)
        {
            await _manager.SetAssignmentAsync(
                extensionId,
                ExtensionScope.Global,
                serviceId: null,
                accountId: null,
                enabled: enabled);

            _notifications.Show(
                enabled ? "Extension Enabled" : "Extension Disabled",
                enabled ? "Enabled globally." : "Disabled globally.",
                NotificationSeverity.Information);
        }

        public async Task RemoveExtensionAsync(string extensionId)
        {
            var ext = _manager.GetExtension(extensionId);
            if (ext is null) return;

            var ok = await _manager.RemoveAsync(extensionId);
            if (ok)
            {
                _notifications.Show(
                    "Extension Removed",
                    $"{ext.Name} has been removed.",
                    NotificationSeverity.Information);
            }
        }

        // ─────────────────────────────────────────────
        //  Reactive
        // ─────────────────────────────────────────────

        partial void OnSearchTextChanged(string value)
            => _ = ReloadCatalogAsync();

        partial void OnSelectedCategoryChanged(string value)
            => _ = ReloadCatalogAsync();

        private void OnExtensionsChanged(object? sender, EventArgs e)
        {
            if (_dispatcherQueue is null) return;

            _dispatcherQueue.TryEnqueue(() =>
            {
                UpdateInstalledFlags();
                RefreshInstalledList();
            });
        }

        private void UpdateInstalledFlags()
        {
            foreach (var card in FeaturedCards)
                card.IsInstalled = IsInstalledByName(card.Item.Name);

            foreach (var card in AllCards)
                card.IsInstalled = IsInstalledByName(card.Item.Name);
        }
    }
}