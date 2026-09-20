using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Storage
{
    public sealed class JsonConfigurationStorage : IConfigurationStorage
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ILogger<JsonConfigurationStorage> _logger;
        private readonly string _configPath;

        public JsonConfigurationStorage(ILogger<JsonConfigurationStorage> logger)
        {
            _logger = logger;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "DIHub");
            Directory.CreateDirectory(folder);
            _configPath = Path.Combine(folder, "config.json");

            TryRestrictFolderPermissions(folder);

            _logger.LogInformation("Configuration path initialized.");
        }

        // ─────────────────────────────────────────────
        //  Load
        // ─────────────────────────────────────────────

        public async Task<AppConfiguration> LoadAsync(CancellationToken ct = default)
        {
            if (!File.Exists(_configPath))
                return new AppConfiguration();

            try
            {
                await using var stream = File.OpenRead(_configPath);
                var config = await JsonSerializer.DeserializeAsync<AppConfiguration>(stream, JsonOptions, ct);

                if (config is null) return new AppConfiguration();

                var needsMigration = config.Version < ConfigurationMigrator.CurrentVersion;
                config = ConfigurationMigrator.Migrate(config, _logger);

                if (needsMigration)
                {
                    try { await SaveAsync(config, ct); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Post-migration save failed: {Error}", ex.GetType().Name);
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to read config file ({Error}). Returning empty config.",
                    ex.GetType().Name);

                try
                {
                    var backup = _configPath + ".broken." + DateTime.Now.ToString("yyyyMMddHHmmss");
                    File.Copy(_configPath, backup, overwrite: false);
                    _logger.LogWarning("Corrupt config backed up.");
                }
                catch { }

                return new AppConfiguration();
            }
        }

        // ─────────────────────────────────────────────
        //  Save
        // ─────────────────────────────────────────────

        public async Task SaveAsync(AppConfiguration config, CancellationToken ct = default)
        {
            try
            {
                var temp = _configPath + ".tmp";

                await using (var stream = File.Create(temp))
                {
                    await JsonSerializer.SerializeAsync(stream, config, JsonOptions, ct);
                }

                // Restrict permissions on the temp file before moving.
                TryRestrictFilePermissions(temp);

                if (File.Exists(_configPath))
                    File.Replace(temp, _configPath, destinationBackupFileName: null);
                else
                    File.Move(temp, _configPath);

                TryRestrictFilePermissions(_configPath);

                _logger.LogDebug("Configuration saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save config: {Error}", ex.GetType().Name);
            }
        }

        // ─────────────────────────────────────────────
        //  ACL hardening
        // ─────────────────────────────────────────────

        /// <summary>
        /// Restricts the DIHub config folder to the current user only.
        /// This is a best-effort operation — failure is not fatal.
        /// </summary>
        private void TryRestrictFolderPermissions(string folder)
        {
            if (!OperatingSystem.IsWindows()) return;

            try
            {
                var info = new DirectoryInfo(folder);
                var security = info.GetAccessControl();

                // Break inheritance and remove inherited rules.
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

                // Remove all existing explicit rules.
                foreach (FileSystemAccessRule rule in security.GetAccessRules(
                    includeExplicit: true, includeInherited: false, targetType: typeof(SecurityIdentifier)))
                {
                    security.RemoveAccessRule(rule);
                }

                var currentUser = WindowsIdentity.GetCurrent().User;
                if (currentUser is null) return;

                security.AddAccessRule(new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                info.SetAccessControl(security);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not restrict folder ACL: {Error}", ex.GetType().Name);
            }
        }

        private void TryRestrictFilePermissions(string path)
        {
            if (!OperatingSystem.IsWindows()) return;
            if (!File.Exists(path)) return;

            try
            {
                var info = new FileInfo(path);
                var security = info.GetAccessControl();

                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

                foreach (FileSystemAccessRule rule in security.GetAccessRules(
                    includeExplicit: true, includeInherited: false, targetType: typeof(SecurityIdentifier)))
                {
                    security.RemoveAccessRule(rule);
                }

                var currentUser = WindowsIdentity.GetCurrent().User;
                if (currentUser is null) return;

                security.AddAccessRule(new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow));

                info.SetAccessControl(security);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not restrict file ACL: {Error}", ex.GetType().Name);
            }
        }
    }
}