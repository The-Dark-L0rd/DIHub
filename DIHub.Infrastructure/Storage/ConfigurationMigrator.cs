using System;
using System.Collections.ObjectModel;
using System.Linq;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Storage
{
    public static class ConfigurationMigrator
    {
        public const int CurrentVersion = 7;

        public static AppConfiguration Migrate(AppConfiguration config, ILogger? logger = null)
        {
            if (config is null) return new AppConfiguration();

            // v1 / v2 → v3 : Add Default account to every service.
            if (config.Version < 3)
            {
                foreach (var svc in config.Services)
                    EnsureDefaultAccount(svc);

                config.Version = 3;
                logger?.LogInformation("Migrated config to v3 (multi-account).");
            }

            // v3 → v4 : Initialize Multi-AI collections.
            if (config.Version < 4)
            {
                config.MultiAIWorkspaces ??= new();
                config.Version = 4;
                logger?.LogInformation("Migrated config to v4 (multi-AI).");
            }

            // v4 → v5 : Presets + Prompt History.
            if (config.Version < 5)
            {
                config.TargetPresets ??= new();
                config.PromptHistory ??= new();
                config.Version = 5;
                logger?.LogInformation("Migrated config to v5 (presets + history).");
            }

            // v5 → v6 : Multi-AI settings (defaulted; no data migration needed).
            if (config.Version < 6)
            {
                config.Version = 6;
                logger?.LogInformation("Migrated config to v6 (multi-AI settings).");
            }

            // v6 → v7 : Window state + session restore (both default to empty).
            if (config.Version < 7)
            {
                config.Version = 7;
                logger?.LogInformation("Migrated config to v7 (window state + session restore).");
            }

            // Defensive: ensure every service has at least one account.
            foreach (var svc in config.Services)
                EnsureDefaultAccount(svc);

            return config;
        }

        private static void EnsureDefaultAccount(AIService svc)
        {
            if (svc.Accounts is null)
                svc.Accounts = new ObservableCollection<AIAccount>();

            if (svc.Accounts.Count == 0)
            {
                svc.Accounts.Add(new AIAccount
                {
                    ServiceId = svc.Id,
                    Name = "Default",
                    Icon = svc.Icon,
                    Accent = svc.Accent,
                    IsDefault = true,
                    Order = 0,
                    ProfileId = Guid.NewGuid().ToString("N")
                });
            }

            if (svc.Accounts.All(a => !a.IsDefault))
                svc.Accounts[0].IsDefault = true;

            foreach (var acc in svc.Accounts)
            {
                if (string.IsNullOrWhiteSpace(acc.ProfileId))
                    acc.ProfileId = Guid.NewGuid().ToString("N");

                acc.ServiceId = svc.Id;
            }
        }
    }
}