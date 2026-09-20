using System.Linq;
using System.Threading.Tasks;
using DIHub.Core.Models;
using DIHub.Core.Services;
using DIHub.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DIHub.Tests.Services
{
    public class AIServiceManagerTests
    {
        private static (AIServiceManager manager, FakeConfigurationStorage storage) Create()
        {
            var storage = new FakeConfigurationStorage();
            var manager = new AIServiceManager(storage, NullLogger<AIServiceManager>.Instance);
            return (manager, storage);
        }

        [Fact]
        public async Task LoadAsync_EmptyConfig_SeedsDefaults()
        {
            var (manager, storage) = Create();

            await manager.LoadAsync();

            Assert.Equal(12, manager.Services.Count);
            Assert.Contains(manager.Services, s => s.Name == "ChatGPT");
            Assert.Contains(manager.Services, s => s.Name == "Claude");
            Assert.Contains(manager.Services, s => s.Name == "Poe");
            Assert.Contains(manager.Services, s => s.Name == "Groq");
            Assert.All(manager.Services, s => Assert.Single(s.Accounts));
        }

        [Fact]
        public async Task LoadAsync_ExistingServices_DoesNotSeed()
        {
            var (manager, storage) = Create();
            storage.Config.Services.Add(new AIService { Name = "MyAI", Url = "https://myai.com" });

            await manager.LoadAsync();

            Assert.Single(manager.Services);
            Assert.Equal("MyAI", manager.Services[0].Name);
        }

        [Fact]
        public void AddService_AssignsDefaultAccount()
        {
            var (manager, _) = Create();

            var svc = manager.AddService(new AIService { Name = "NewAI", Url = "https://newai.com" });

            Assert.Single(svc.Accounts);
            Assert.True(svc.Accounts[0].IsDefault);
            Assert.Equal("Default", svc.Accounts[0].Name);
        }

        [Fact]
        public void AddService_GeneratesIdIfMissing()
        {
            var (manager, _) = Create();

            var svc = manager.AddService(new AIService { Name = "X", Url = "https://x.com" });

            Assert.False(string.IsNullOrWhiteSpace(svc.Id));
        }

        [Fact]
        public void RemoveService_RemovesMatchingService()
        {
            var (manager, _) = Create();
            var svc = manager.AddService(new AIService { Name = "Temp", Url = "https://t.com" });

            manager.RemoveService(svc.Id);

            Assert.DoesNotContain(manager.Services, s => s.Id == svc.Id);
        }

        [Fact]
        public void ToggleFavorite_TogglesFlag()
        {
            var (manager, _) = Create();
            var svc = manager.AddService(new AIService { Name = "A", Url = "https://a.com" });

            manager.ToggleFavorite(svc.Id);
            Assert.True(manager.FindService(svc.Id)!.Favorite);

            manager.ToggleFavorite(svc.Id);
            Assert.False(manager.FindService(svc.Id)!.Favorite);
        }

        [Fact]
        public void UpdateService_ModifiesFields()
        {
            var (manager, _) = Create();
            var svc = manager.AddService(new AIService { Name = "Old", Url = "https://old.com" });

            svc.Name = "New";
            svc.Url = "https://new.com";
            manager.UpdateService(svc);

            var result = manager.FindService(svc.Id);
            Assert.Equal("New", result!.Name);
            Assert.Equal("https://new.com", result.Url);
        }

        [Fact]
        public void FindService_ReturnsNullForUnknownId()
        {
            var (manager, _) = Create();

            Assert.Null(manager.FindService("nonexistent"));
        }

        [Fact]
        public void Sort_PutsFavoritesFirst()
        {
            var (manager, _) = Create();
            var a = manager.AddService(new AIService { Name = "A", Url = "https://a.com" });
            var b = manager.AddService(new AIService { Name = "B", Url = "https://b.com" });

            manager.ToggleFavorite(b.Id);

            Assert.Equal(b.Id, manager.Services[0].Id);
            Assert.Equal(a.Id, manager.Services[1].Id);
        }

        [Fact]
        public void ResetToDefaults_SeedsTwelveServices()
        {
            var (manager, _) = Create();
            manager.AddService(new AIService { Name = "Custom", Url = "https://custom.com" });

            manager.ResetToDefaults();

            Assert.Equal(12, manager.Services.Count);
            Assert.DoesNotContain(manager.Services, s => s.Name == "Custom");
        }

        [Fact]
        public void ServicesChanged_FiresOnAdd()
        {
            var (manager, _) = Create();
            var fired = false;
            manager.ServicesChanged += (s, e) => fired = true;

            manager.AddService(new AIService { Name = "X", Url = "https://x.com" });

            Assert.True(fired);
        }
    }
}