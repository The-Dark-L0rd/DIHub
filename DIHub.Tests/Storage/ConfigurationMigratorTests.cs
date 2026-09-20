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

            Assert.Single(result.Services[0].Accounts);
            Assert.True(result.Services[0].Accounts[0].IsDefault);
            Assert.Equal("Default", result.Services[0].Accounts[0].Name);
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

            Assert.Single(result.Services);
            Assert.Equal("Custom", result.Services[0].Name);
            Assert.Equal("Personal", result.Services[0].Accounts[0].Name);
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

            Assert.False(string.IsNullOrWhiteSpace(result.Services[0].Accounts[0].ProfileId));
        }
    }
}