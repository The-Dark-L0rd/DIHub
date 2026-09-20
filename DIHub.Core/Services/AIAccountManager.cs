using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Core.Services
{
    public sealed class AIAccountManager : IAIAccountManager
    {
        private readonly IAIServiceManager _services;
        private readonly ILogger<AIAccountManager> _logger;
        private readonly string _profilesRoot;

        public event EventHandler? AccountsChanged;

        public AIAccountManager(IAIServiceManager services, ILogger<AIAccountManager> logger)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _profilesRoot = Path.Combine(localAppData, "DIHub", "Profiles");
            Directory.CreateDirectory(_profilesRoot);
        }

        public IReadOnlyList<AIAccount> AllAccounts
            => _services.Services.SelectMany(s => s.Accounts).ToList();

        public IReadOnlyList<AIAccount> GetAccountsForService(string serviceId)
        {
            var svc = _services.FindService(serviceId);
            return svc?.Accounts ?? new ObservableCollection<AIAccount>();
        }

        public AIAccount? GetAccount(string accountId)
            => AllAccounts.FirstOrDefault(a => a.Id == accountId);

        public Task LoadAsync(CancellationToken ct = default)
        {
            AccountsChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task SaveAsync(CancellationToken ct = default)
            => _services.SaveAsync(ct);

        public AIAccount AddAccount(string serviceId, string name, string? icon = null)
        {
            var svc = _services.FindService(serviceId)
                ?? throw new InvalidOperationException($"Service '{serviceId}' not found.");

            var account = new AIAccount
            {
                ServiceId = svc.Id,
                Name = string.IsNullOrWhiteSpace(name) ? "New Account" : name.Trim(),
                Icon = icon ?? svc.Icon,
                Accent = svc.Accent,
                Order = svc.Accounts.Count == 0 ? 0 : svc.Accounts.Max(a => a.Order) + 1,
                IsDefault = svc.Accounts.Count == 0
            };

            svc.Accounts.Add(account);
            _ = SaveAsync();
            _ = EnsureProfileAsync(account.Id);
            AccountsChanged?.Invoke(this, EventArgs.Empty);
            return account;
        }

        public void UpdateAccount(AIAccount account)
        {
            var existing = GetAccount(account.Id);
            if (existing is null) return;

            existing.Name = account.Name;
            existing.Icon = account.Icon;
            existing.Accent = account.Accent;
            existing.Enabled = account.Enabled;
            existing.Favorite = account.Favorite;
            existing.Pinned = account.Pinned;
            existing.IsDefault = account.IsDefault;
            existing.OpenInExternalBrowser = account.OpenInExternalBrowser;

            _ = SaveAsync();
            AccountsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveAccount(string accountId)
            => _ = DeleteAccountAsync(accountId);

        public void RenameAccount(string accountId, string newName)
        {
            var acc = GetAccount(accountId);
            if (acc is null) return;

            acc.Name = string.IsNullOrWhiteSpace(newName) ? acc.Name : newName.Trim();
            _ = SaveAsync();
            AccountsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetDefaultAccount(string accountId)
        {
            var acc = GetAccount(accountId);
            if (acc is null) return;

            var svc = _services.FindService(acc.ServiceId);
            if (svc is null) return;

            foreach (var a in svc.Accounts)
                a.IsDefault = a.Id == accountId;

            _ = SaveAsync();
            AccountsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleFavorite(string accountId)
        {
            var acc = GetAccount(accountId);
            if (acc is null) return;
            acc.Favorite = !acc.Favorite;
            _ = SaveAsync();
            AccountsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void TogglePinned(string accountId)
        {
            var acc = GetAccount(accountId);
            if (acc is null) return;
            acc.Pinned = !acc.Pinned;
            _ = SaveAsync();
            AccountsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ReorderAccounts(string serviceId, IEnumerable<string> orderedIds)
        {
            var svc = _services.FindService(serviceId);
            if (svc is null) return;

            var order = 0;
            foreach (var id in orderedIds)
            {
                var acc = svc.Accounts.FirstOrDefault(a => a.Id == id);
                if (acc is not null) acc.Order = order++;
            }

            _ = SaveAsync();
            AccountsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MarkUsed(string accountId)
        {
            var acc = GetAccount(accountId);
            if (acc is null) return;
            acc.LastUsedAt = DateTime.UtcNow;
            _ = SaveAsync();
        }

        // ─────────────────────────────────────────────
        //  Profile storage
        // ─────────────────────────────────────────────

        public string GetProfilePath(string accountId)
        {
            var acc = GetAccount(accountId);
            if (acc is null) throw new InvalidOperationException($"Account '{accountId}' not found.");

            var svc = _services.FindService(acc.ServiceId);
            var safeServiceName = SanitizeFolderName(svc?.Name ?? "Service");
            var safeProfileId = SanitizeFolderName(acc.ProfileId);

            return Path.Combine(_profilesRoot, safeServiceName, safeProfileId);
        }

        public Task EnsureProfileAsync(string accountId, CancellationToken ct = default)
        {
            try
            {
                var path = GetProfilePath(accountId);
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure profile for {AccountId}.", accountId);
            }
            return Task.CompletedTask;
        }

        public async Task ClearProfileAsync(string accountId, CancellationToken ct = default)
        {
            try
            {
                var path = GetProfilePath(accountId);
                if (!Directory.Exists(path)) return;

                await Task.Run(() =>
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    foreach (var dir in Directory.EnumerateDirectories(path))
                    {
                        try { Directory.Delete(dir, recursive: true); } catch { }
                    }
                }, ct);

                _logger.LogInformation("Profile cleared for account {AccountId}.", accountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear profile for {AccountId}.", accountId);
            }
        }

        public async Task DeleteAccountAsync(string accountId, CancellationToken ct = default)
        {
            var acc = GetAccount(accountId);
            if (acc is null) return;

            var svc = _services.FindService(acc.ServiceId);
            if (svc is null) return;

            if (svc.Accounts.Count <= 1)
            {
                _logger.LogWarning("Cannot delete the last account of {Service}.", svc.Name);
                return;
            }

            try
            {
                var path = GetProfilePath(accountId);
                if (Directory.Exists(path))
                    await Task.Run(() => Directory.Delete(path, recursive: true), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete profile folder.");
            }

            svc.Accounts.Remove(acc);

            if (acc.IsDefault && svc.Accounts.Count > 0)
                svc.Accounts[0].IsDefault = true;

            await SaveAsync(ct);
            AccountsChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task DeleteAllProfilesForServiceAsync(string serviceId, CancellationToken ct = default)
        {
            var svc = _services.FindService(serviceId);
            if (svc is null) return;

            try
            {
                var safeServiceName = SanitizeFolderName(svc.Name);
                var serviceRoot = Path.Combine(_profilesRoot, safeServiceName);

                if (Directory.Exists(serviceRoot))
                {
                    await Task.Run(() => Directory.Delete(serviceRoot, recursive: true), ct);
                    _logger.LogInformation("Deleted all profiles for {Service}.", svc.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete all profiles for {Service}.", svc.Name);
            }
        }

        private static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "unnamed";

            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "unnamed" : cleaned;
        }
    }
}