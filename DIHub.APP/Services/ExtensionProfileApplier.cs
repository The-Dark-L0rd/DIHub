using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace DIHub.APP.Services
{
    public sealed class ExtensionProfileApplier : IExtensionProfileApplier, IDisposable
    {
        private readonly IExtensionManager _manager;
        private readonly IAIAccountManager _accounts;
        private readonly ILogger<ExtensionProfileApplier> _logger;

        private readonly Dictionary<string, CoreWebView2Profile> _liveProfiles = new();

        public ExtensionProfileApplier(
            IExtensionManager manager,
            IAIAccountManager accounts,
            ILogger<ExtensionProfileApplier> logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Whenever the manager's config changes (install / remove / assignment),
            // resync live profiles asynchronously.
            _manager.ExtensionsChanged += OnExtensionsChanged;
        }

        public void RegisterProfile(string accountId, CoreWebView2Profile profile)
        {
            if (string.IsNullOrEmpty(accountId) || profile is null) return;
            _liveProfiles[accountId] = profile;
            _logger.LogDebug("Profile registered: {AccountId}", accountId);
        }

        public void UnregisterProfile(string accountId)
        {
            if (string.IsNullOrEmpty(accountId)) return;
            if (_liveProfiles.Remove(accountId))
                _logger.LogDebug("Profile unregistered: {AccountId}", accountId);
        }

        public async Task ApplyExtensionsToProfileAsync(
            string accountId, CancellationToken ct = default)
        {
            if (!_liveProfiles.TryGetValue(accountId, out var profile)) return;
            if (profile is null) return;

            var account = _accounts.GetAccount(accountId);
            if (account is null) return;

            var serviceId = account.ServiceId;

            // Snapshot of what's already installed on the profile (by name).
            HashSet<string> installedNames;
            try
            {
                var installedList = await profile.GetBrowserExtensionsAsync();
                installedNames = installedList
                    .Select(b => b.Name ?? string.Empty)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Could not enumerate existing extensions on profile {Account}", accountId);
                installedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var ext in _manager.InstalledExtensions.ToList())
            {
                ct.ThrowIfCancellationRequested();

                var state = _manager.GetEffectiveState(ext.Id, serviceId, accountId);
                if (!state.IsEnabled) continue;

                if (string.IsNullOrEmpty(ext.InstallPath) || !Directory.Exists(ext.InstallPath))
                    continue;

                try
                {
                    if (installedNames.Contains(ext.Name))
                    {
                        // Already installed — make sure it's enabled.
                        var installedList = await profile.GetBrowserExtensionsAsync();
                        var match = installedList.FirstOrDefault(b =>
                            string.Equals(b.Name, ext.Name, StringComparison.OrdinalIgnoreCase));
                        if (match is not null && !match.IsEnabled)
                        {
                            // ── FIX: EnableAsync requires a bool argument in this SDK.
                            await match.EnableAsync(true);
                        }
                        continue;
                    }

                    var handle = await profile.AddBrowserExtensionAsync(ext.InstallPath);

                    // ── FIX: EnableAsync requires a bool argument in this SDK.
                    await handle.EnableAsync(true);

                    installedNames.Add(ext.Name);

                    _logger.LogInformation(
                        "Extension {Name} installed on profile {Account}",
                        ext.Name, accountId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to install extension {Name} on profile {Account}",
                        ext.Name, accountId);
                }
            }
        }

        public async Task SyncAllLiveProfilesAsync(CancellationToken ct = default)
        {
            foreach (var accountId in _liveProfiles.Keys.ToList())
            {
                try { await ApplyExtensionsToProfileAsync(accountId, ct); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to sync extensions to profile {Account}", accountId);
                }
            }
        }

        private void OnExtensionsChanged(object? sender, EventArgs e)
        {
            // Fire-and-forget — never block the caller (usually UI thread).
            _ = Task.Run(async () =>
            {
                try { await SyncAllLiveProfilesAsync(); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Background extension sync failed.");
                }
            });
        }

        public void Dispose()
        {
            _manager.ExtensionsChanged -= OnExtensionsChanged;
            _liveProfiles.Clear();
        }
    }
}