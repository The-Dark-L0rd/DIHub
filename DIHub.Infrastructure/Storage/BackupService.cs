using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Storage
{
    public sealed class BackupService : IBackupService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IConfigurationStorage _configStorage;
        private readonly IExtensionStorageService _extensionStorage;
        private readonly ISettingsService _settings;
        private readonly ILogger<BackupService> _logger;

        public BackupService(
            IConfigurationStorage configStorage,
            IExtensionStorageService extensionStorage,
            ISettingsService settings,
            ILogger<BackupService> logger)
        {
            _configStorage = configStorage;
            _extensionStorage = extensionStorage;
            _settings = settings;
            _logger = logger;
        }

        // ─────────────────────────────────────────────
        //  Export
        // ─────────────────────────────────────────────

        public async Task ExportAsync(
            string destinationPath,
            bool includeServices,
            bool includeExtensions,
            bool includeSettings,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("destinationPath is required.", nameof(destinationPath));

            var included = new System.Collections.Generic.List<string>();

            var backup = new BackupFile
            {
                CreatedAt = DateTime.UtcNow,
                AppVersion = "3.12.0"
            };

            if (includeServices)
            {
                try
                {
                    backup.Configuration = await _configStorage.LoadAsync(ct);
                    included.Add("Services");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to include services in backup.");
                }
            }

            if (includeExtensions)
            {
                try
                {
                    backup.Extensions = await _extensionStorage.LoadAsync(ct);
                    included.Add("Extensions");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to include extensions in backup.");
                }
            }

            if (includeSettings)
            {
                backup.Settings = SnapshotSettings();
                included.Add("Settings");
            }

            backup.IncludedSections = included.ToArray();

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await using (var stream = File.Create(destinationPath))
            {
                await JsonSerializer.SerializeAsync(stream, backup, JsonOptions, ct);
            }

            _logger.LogInformation("Backup written to {Path}", destinationPath);
        }

        private BackupSettingsSection SnapshotSettings()
        {
            var s = _settings.Current;

            return new BackupSettingsSection
            {
                RestorePreviousSession = s.RestorePreviousSession,
                ConfirmOnCloseMultipleTabs = s.ConfirmOnCloseMultipleTabs,
                RunInBackground = s.RunInBackground,

                Theme = s.Theme,
                Accent = s.Accent,
                SidebarExpandedByDefault = s.SidebarExpandedByDefault,
                AnimationsEnabled = s.AnimationsEnabled,

                OpenLinks = s.OpenLinks,
                ExternalBrowser = s.ExternalBrowser,
                SearchEngine = s.SearchEngine,

                MultiAIDefaultPanelCount = s.MultiAIDefaultPanelCount,
                MultiAIConfirmMultiSend = s.MultiAIConfirmMultiSend,
                MultiAIKeepTargetsForNext = s.MultiAIKeepTargetsForNext,
                MultiAIRememberLastTargets = s.MultiAIRememberLastTargets,
                MultiAIAutoFocusActivePanel = s.MultiAIAutoFocusActivePanel,
                MultiAIAutomaticDispatch = s.MultiAIAutomaticDispatch,
                MultiAIAllowProviderAutomation = s.MultiAIAllowProviderAutomation,

                ExtensionsEnabled = s.ExtensionsEnabled,
                AllowLocalExtensions = s.AllowLocalExtensions,
                AllowRemoteExtensions = s.AllowRemoteExtensions,
                ExtensionConfirmInstall = s.ExtensionConfirmInstall,
                ExtensionShowPermissionWarnings = s.ExtensionShowPermissionWarnings,
                ExtensionSafeMode = s.ExtensionSafeMode
            };
        }

        // ─────────────────────────────────────────────
        //  Read
        // ─────────────────────────────────────────────

        public async Task<BackupFile?> ReadAsync(string sourcePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return null;

            try
            {
                await using var stream = File.OpenRead(sourcePath);
                var backup = await JsonSerializer.DeserializeAsync<BackupFile>(stream, JsonOptions, ct);
                return backup;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read backup file {Path}", sourcePath);
                return null;
            }
        }

        // ─────────────────────────────────────────────
        //  Apply
        // ─────────────────────────────────────────────

        public async Task ApplyAsync(BackupFile backup, CancellationToken ct = default)
        {
            if (backup is null) return;

            // ── Services ──
            if (backup.Configuration is not null)
            {
                try
                {
                    await _configStorage.SaveAsync(backup.Configuration, ct);
                    _logger.LogInformation("Backup: services restored.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to restore services from backup.");
                }
            }

            // ── Extensions ──
            if (backup.Extensions is not null)
            {
                try
                {
                    await _extensionStorage.SaveAsync(backup.Extensions, ct);
                    _logger.LogInformation("Backup: extensions restored.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to restore extensions from backup.");
                }
            }

            // ── Settings ──
            if (backup.Settings is not null)
            {
                try
                {
                    ApplySettingsSection(backup.Settings);
                    await _settings.SaveAsync(ct);
                    _logger.LogInformation("Backup: settings restored.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to restore settings from backup.");
                }
            }
        }

        private void ApplySettingsSection(BackupSettingsSection s)
        {
            _settings.Update(cur =>
            {
                if (s.RestorePreviousSession.HasValue) cur.RestorePreviousSession = s.RestorePreviousSession.Value;
                if (s.ConfirmOnCloseMultipleTabs.HasValue) cur.ConfirmOnCloseMultipleTabs = s.ConfirmOnCloseMultipleTabs.Value;
                if (s.RunInBackground.HasValue) cur.RunInBackground = s.RunInBackground.Value;

                if (s.Theme.HasValue) cur.Theme = s.Theme.Value;
                if (s.Accent.HasValue) cur.Accent = s.Accent.Value;
                if (s.SidebarExpandedByDefault.HasValue) cur.SidebarExpandedByDefault = s.SidebarExpandedByDefault.Value;
                if (s.AnimationsEnabled.HasValue) cur.AnimationsEnabled = s.AnimationsEnabled.Value;

                if (s.OpenLinks.HasValue) cur.OpenLinks = s.OpenLinks.Value;
                if (s.ExternalBrowser.HasValue) cur.ExternalBrowser = s.ExternalBrowser.Value;
                if (s.SearchEngine.HasValue) cur.SearchEngine = s.SearchEngine.Value;

                if (s.MultiAIDefaultPanelCount.HasValue) cur.MultiAIDefaultPanelCount = s.MultiAIDefaultPanelCount.Value;
                if (s.MultiAIConfirmMultiSend.HasValue) cur.MultiAIConfirmMultiSend = s.MultiAIConfirmMultiSend.Value;
                if (s.MultiAIKeepTargetsForNext.HasValue) cur.MultiAIKeepTargetsForNext = s.MultiAIKeepTargetsForNext.Value;
                if (s.MultiAIRememberLastTargets.HasValue) cur.MultiAIRememberLastTargets = s.MultiAIRememberLastTargets.Value;
                if (s.MultiAIAutoFocusActivePanel.HasValue) cur.MultiAIAutoFocusActivePanel = s.MultiAIAutoFocusActivePanel.Value;
                if (s.MultiAIAutomaticDispatch.HasValue) cur.MultiAIAutomaticDispatch = s.MultiAIAutomaticDispatch.Value;
                if (s.MultiAIAllowProviderAutomation.HasValue) cur.MultiAIAllowProviderAutomation = s.MultiAIAllowProviderAutomation.Value;

                if (s.ExtensionsEnabled.HasValue) cur.ExtensionsEnabled = s.ExtensionsEnabled.Value;
                if (s.AllowLocalExtensions.HasValue) cur.AllowLocalExtensions = s.AllowLocalExtensions.Value;
                if (s.AllowRemoteExtensions.HasValue) cur.AllowRemoteExtensions = s.AllowRemoteExtensions.Value;
                if (s.ExtensionConfirmInstall.HasValue) cur.ExtensionConfirmInstall = s.ExtensionConfirmInstall.Value;
                if (s.ExtensionShowPermissionWarnings.HasValue) cur.ExtensionShowPermissionWarnings = s.ExtensionShowPermissionWarnings.Value;
                if (s.ExtensionSafeMode.HasValue) cur.ExtensionSafeMode = s.ExtensionSafeMode.Value;
            });
        }
    }
}