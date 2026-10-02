using System.Linq;
using DIHub.Core.Models;
using DIHub.Infrastructure.Storage;
using Xunit;

namespace DIHub.Tests.Storage
{
    public class ConfigurationMigratorTests
    {
        [Fact]
        public void Migrate_OldConfig_BumpsVersion()
        {
            var config = new AppConfiguration { Version = 2 };

            var result = ConfigurationMigrator.Migrate(config);

            Assert.Equal(ConfigurationMigrator.CurrentVersion, result.Version);
        }

        [Fact]
        public void Migrate_AddsDefaultAccount_WhenMissing()
        {
            var config = new AppConfiguration
            {
                Version = 2,
                Services = { new AIService { Name = "ChatGPT", Url = "https://chatgpt.com/" } }
            };

            var result = ConfigurationMigrator.Migrate(config);

            // Note: v7→v8 migration appends Qwen, so the list may contain more
            // than one service. Find the original one by name.
            var chatgpt = result.Services.FirstOrDefault(s => s.Name == "ChatGPT");
            Assert.NotNull(chatgpt);
            Assert.Single(chatgpt!.Accounts);
            Assert.True(chatgpt.Accounts[0].IsDefault);
            Assert.Equal("Default", chatgpt.Accounts[0].Name);
        }

        [Fact]
        public void Migrate_InitializesMultiAICollections()
        {
            var config = new AppConfiguration { Version = 3 };

            var result = ConfigurationMigrator.Migrate(config);

            Assert.NotNull(result.MultiAIWorkspaces);
            Assert.NotNull(result.TargetPresets);
            Assert.NotNull(result.PromptHistory);
        }

        [Fact]
        public void Migrate_PreservesExistingData()
        {
            var config = new AppConfiguration
            {
                Version = 3,
                Services =
                {
                    new AIService
                    {
                        Name = "Custom",
                        Url = "https://custom.com",
                        Accounts = { new AIAccount { Name = "Personal" } }
                    }
                }
            };

            var result = ConfigurationMigrator.Migrate(config);

            // Original service is preserved and Qwen is appended by v8 migration.
            var custom = result.Services.FirstOrDefault(s => s.Name == "Custom");
            Assert.NotNull(custom);
            Assert.Equal("https://custom.com", custom!.Url);
            Assert.Contains(custom.Accounts, a => a.Name == "Personal");

            // Qwen should also be present now.
            Assert.Contains(result.Services, s => s.Name == "Qwen");
        }

        [Fact]
        public void Migrate_CurrentVersion_NoChange()
        {
            var config = new AppConfiguration { Version = ConfigurationMigrator.CurrentVersion };

            var result = ConfigurationMigrator.Migrate(config);

            Assert.Equal(ConfigurationMigrator.CurrentVersion, result.Version);
        }

        [Fact]
        public void Migrate_Null_ReturnsEmpty()
        {
            var result = ConfigurationMigrator.Migrate(null!);

            Assert.NotNull(result);
            Assert.Equal(ConfigurationMigrator.CurrentVersion, result.Version);
        }

        [Fact]
        public void Migrate_EnsuresProfileId()
        {
            var config = new AppConfiguration
            {
                Version = 3,
                Services =
                {
                    new AIService
                    {
                        Name = "X",
                        Url = "https://x.com",
                        Accounts = { new AIAccount { Name = "A", ProfileId = "" } }
                    }
                }
            };

            var result = ConfigurationMigrator.Migrate(config);

            var x = result.Services.FirstOrDefault(s => s.Name == "X");
            Assert.NotNull(x);
            Assert.False(string.IsNullOrWhiteSpace(x!.Accounts[0].ProfileId));
        }

        // ─────────────────────────────────────────────
        //  NEW: v7 → v8 specific tests
        // ─────────────────────────────────────────────

        [Fact]
        public void Migrate_V7ToV8_AddsQwen()
        {
            var config = new AppConfiguration
            {
                Version = 7,
                Services =
                {
                    new AIService { Name = "ChatGPT", Url = "https://chatgpt.com/" }
                }
            };

            var result = ConfigurationMigrator.Migrate(config);

            Assert.Equal(8, result.Version);
            Assert.Contains(result.Services, s => s.Name == "Qwen");
        }

        [Fact]
        public void Migrate_V7ToV8_DoesNotDuplicateQwen()
        {
            var config = new AppConfiguration
            {
                Version = 7,
                Services =
                {
                    new AIService { Name = "Qwen", Url = "https://chat.qwen.ai/" }
                }
            };

            var result = ConfigurationMigrator.Migrate(config);

            // Should not add a second Qwen.
            Assert.Single(result.Services.Where(s => s.Name == "Qwen"));
        }

        [Fact]
        public void Migrate_V7ToV8_QwenHasDefaultAccount()
        {
            var config = new AppConfiguration { Version = 7 };

            var result = ConfigurationMigrator.Migrate(config);

            var qwen = result.Services.FirstOrDefault(s => s.Name == "Qwen");
            Assert.NotNull(qwen);
            Assert.Single(qwen!.Accounts);
            Assert.True(qwen.Accounts[0].IsDefault);
        }
    }
}