using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DIHub.Infrastructure.Storage
{
    public static class ConfigurationMigrator
    {
        public const int CurrentVersion = 8;

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

            // v7 → v8 : Add Qwen to the default AI services if it is missing.
            //           We never remove existing services — only append.
            if (config.Version < 8)
            {
                TryAddQwen(config, logger);
                config.Version = 8;
                logger?.LogInformation("Migrated config to v8 (Qwen default service).");
            }

            // Defensive: ensure every service has at least one account.
            foreach (var svc in config.Services)
                EnsureDefaultAccount(svc);

            return config;
        }

        private static void TryAddQwen(AppConfiguration config, ILogger? logger)
        {
            // Skip if a Qwen service already exists (either by name or URL).
            var alreadyPresent = config.Services.Any(s =>
                string.Equals(s.Name, "Qwen", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.Url, "https://chat.qwen.ai/", StringComparison.OrdinalIgnoreCase));

            if (alreadyPresent) return;

            var qwen = new AIService
            {
                Name = "Qwen",
                Url = "https://chat.qwen.ai/",
                Icon = "\uE99A",
                Accent = AccentColor.Purple,
                Order = config.Services.Count == 0
                    ? 0
                    : config.Services.Max(s => s.Order) + 1
            };

            qwen.Accounts.Add(new AIAccount
            {
                ServiceId = qwen.Id,
                Name = "Default",
                Icon = qwen.Icon,
                Accent = qwen.Accent,
                IsDefault = true
            });

            config.Services.Add(qwen);
            logger?.LogInformation("Added Qwen to the AI services list (v8 migration).");
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