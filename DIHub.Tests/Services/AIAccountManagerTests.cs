using System.Linq;
using System.Threading.Tasks;
using DIHub.Core.Models;
using DIHub.Core.Services;
using DIHub.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DIHub.Tests.Services
{
    public class AIAccountManagerTests
    {
        private static (AIAccountManager accounts, AIServiceManager services) Create()
        {
            var storage = new FakeConfigurationStorage();
            var services = new AIServiceManager(storage, NullLogger<AIServiceManager>.Instance);
            var accounts = new AIAccountManager(services, NullLogger<AIAccountManager>.Instance);
            return (accounts, services);
        }

        [Fact]
        public void AddAccount_FirstAccount_IsDefault()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "X", Url = "https://x.com" });

            // Remove the auto-created default so this is truly the first.
            svc.Accounts.Clear();

            var acc = accounts.AddAccount(svc.Id, "Personal");

            Assert.True(acc.IsDefault);
            Assert.Equal("Personal", acc.Name);
        }

        [Fact]
        public void AddAccount_SecondAccount_NotDefault()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "X", Url = "https://x.com" });
            var first = svc.Accounts[0];

            var second = accounts.AddAccount(svc.Id, "Work");

            Assert.False(second.IsDefault);
            Assert.True(first.IsDefault);
        }

        [Fact]
        public void AddAccount_UsesServiceIconWhenNotProvided()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "X", Url = "https://x.com", Icon = "\uE999" });

            var acc = accounts.AddAccount(svc.Id, "Personal");

            Assert.Equal("\uE999", acc.Icon);
        }

        [Fact]
        public void RenameAccount_UpdatesName()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "X", Url = "https://x.com" });
            var acc = svc.Accounts[0];

            accounts.RenameAccount(acc.Id, "Renamed");

            Assert.Equal("Renamed", acc.Name);
        }

        [Fact]
        public void SetDefaultAccount_ChangesDefault()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "X", Url = "https://x.com" });
            var first = svc.Accounts[0];
            var second = accounts.AddAccount(svc.Id, "Second");

            accounts.SetDefaultAccount(second.Id);

            Assert.False(first.IsDefault);
            Assert.True(second.IsDefault);
        }

        [Fact]
        public void ToggleFavorite_TogglesFlag()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "X", Url = "https://x.com" });
            var acc = svc.Accounts[0];

            accounts.ToggleFavorite(acc.Id);
            Assert.True(acc.Favorite);

            accounts.ToggleFavorite(acc.Id);
            Assert.False(acc.Favorite);
        }

        [Fact]
        public void GetAccountsForService_ReturnsCorrectList()
        {
            var (accounts, services) = Create();
            var svcA = services.AddService(new AIService { Name = "A", Url = "https://a.com" });
            var svcB = services.AddService(new AIService { Name = "B", Url = "https://b.com" });
            accounts.AddAccount(svcA.Id, "A2");

            var result = accounts.GetAccountsForService(svcA.Id);

            Assert.Equal(2, result.Count);
            Assert.All(result, a => Assert.Equal(svcA.Id, a.ServiceId));
        }

        [Fact]
        public void GetAccount_FindsAcrossServices()
        {
            var (accounts, services) = Create();
            var svcA = services.AddService(new AIService { Name = "A", Url = "https://a.com" });
            var svcB = services.AddService(new AIService { Name = "B", Url = "https://b.com" });
            var target = svcB.Accounts[0];

            var found = accounts.GetAccount(target.Id);

            Assert.NotNull(found);
            Assert.Equal(svcB.Id, found!.ServiceId);
        }

        [Fact]
        public void GetProfilePath_ContainsServiceNameAndProfileId()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "MyService", Url = "https://x.com" });
            var acc = svc.Accounts[0];

            var path = accounts.GetProfilePath(acc.Id);

            Assert.Contains("MyService", path);
            Assert.Contains(acc.ProfileId, path);
        }

        [Fact]
        public void GetProfilePath_SanitizesServiceName()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "Bad/Name:With*Chars", Url = "https://x.com" });
            var acc = svc.Accounts[0];

            var path = accounts.GetProfilePath(acc.Id);

            Assert.DoesNotContain("/", System.IO.Path.GetFileName(path));
            Assert.DoesNotContain(":", System.IO.Path.GetFileName(path));
        }

        [Fact]
        public async Task DeleteAccountAsync_RefusesLastAccount()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "X", Url = "https://x.com" });
            var only = svc.Accounts[0];

            await accounts.DeleteAccountAsync(only.Id);

            Assert.Single(svc.Accounts);
        }

        [Fact]
        public async Task DeleteAccountAsync_RemovesAndReassignsDefault()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "X", Url = "https://x.com" });
            var first = svc.Accounts[0];
            var second = accounts.AddAccount(svc.Id, "Second");

            await accounts.DeleteAccountAsync(first.Id);

            Assert.Single(svc.Accounts);
            Assert.Equal(second.Id, svc.Accounts[0].Id);
            Assert.True(svc.Accounts[0].IsDefault);
        }

        [Fact]
        public void MarkUsed_UpdatesTimestamp()
        {
            var (accounts, services) = Create();
            var svc = services.AddService(new AIService { Name = "X", Url = "https://x.com" });
            var acc = svc.Accounts[0];
            var before = acc.LastUsedAt;

            System.Threading.Thread.Sleep(20);
            accounts.MarkUsed(acc.Id);

            Assert.True(acc.LastUsedAt > before);
        }
    }
}