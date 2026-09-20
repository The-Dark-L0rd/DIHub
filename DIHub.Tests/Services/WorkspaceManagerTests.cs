using System.Threading.Tasks;
using DIHub.Core.Services;
using DIHub.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DIHub.Tests.Services
{
    public class WorkspaceManagerTests
    {
        private static (WorkspaceManager manager, FakeConfigurationStorage storage) Create()
        {
            var storage = new FakeConfigurationStorage();
            var manager = new WorkspaceManager(storage, NullLogger<WorkspaceManager>.Instance);
            return (manager, storage);
        }

        [Fact]
        public async Task LoadAsync_EmptyConfig_SeedsDefaults()
        {
            var (manager, _) = Create();

            await manager.LoadAsync();

            Assert.Equal(4, manager.Workspaces.Count);
            Assert.NotNull(manager.ActiveWorkspace);
        }

        [Fact]
        public void CreateWorkspace_AddsAndActivates()
        {
            var (manager, _) = Create();
            manager.LoadAsync().Wait();

            var ws = manager.CreateWorkspace("Test");

            Assert.Contains(ws, manager.Workspaces);
            Assert.Equal(ws.Id, manager.ActiveWorkspace!.Id);
        }

        [Fact]
        public void DeleteWorkspace_RefusesLastOne()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace("Only");
            // Remove others by deleting them (but keep at least one).
            while (manager.Workspaces.Count > 1)
                manager.DeleteWorkspace(manager.Workspaces[manager.Workspaces.Count - 1].Id);

            manager.DeleteWorkspace(manager.Workspaces[0].Id);

            Assert.Single(manager.Workspaces);
        }

        [Fact]
        public void RenameWorkspace_UpdatesName()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace("Old");

            manager.RenameWorkspace(ws.Id, "New");

            Assert.Equal("New", manager.Workspaces[0].Name);
        }

        [Fact]
        public void SwitchWorkspace_ChangesActive()
        {
            var (manager, _) = Create();
            manager.LoadAsync().Wait();
            var a = manager.Workspaces[0];
            var b = manager.Workspaces[1];

            manager.SwitchWorkspace(b.Id);

            Assert.Equal(b.Id, manager.ActiveWorkspace!.Id);
        }

        [Fact]
        public void CaptureCurrentTabs_UpdatesActiveWorkspace()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace("Test");
            var urls = new[] { "https://a.com", "https://b.com" };

            manager.CaptureCurrentTabs(urls, "https://a.com");

            Assert.Equal(2, ws.OpenTabs.Count);
            Assert.Equal("https://a.com", ws.ActiveTabUrl);
        }
    }
}