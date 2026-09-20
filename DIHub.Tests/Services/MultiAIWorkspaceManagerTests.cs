using System.Linq;
using System.Threading.Tasks;
using DIHub.Core.Models;
using DIHub.Core.Services;
using DIHub.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DIHub.Tests.Services
{
    public class MultiAIWorkspaceManagerTests
    {
        private static (MultiAIWorkspaceManager manager, FakeConfigurationStorage storage) Create()
        {
            var storage = new FakeConfigurationStorage();
            var manager = new MultiAIWorkspaceManager(storage, NullLogger<MultiAIWorkspaceManager>.Instance);
            return (manager, storage);
        }

        [Theory]
        [InlineData(2, MultiAILayoutMode.TwoByOne)]
        [InlineData(3, MultiAILayoutMode.ThreeTopTwo)]
        [InlineData(4, MultiAILayoutMode.FourGrid)]
        [InlineData(1, MultiAILayoutMode.TwoByOne)]
        [InlineData(99, MultiAILayoutMode.TwoByOne)]
        public void ComputeLayout_ReturnsExpected(int count, MultiAILayoutMode expected)
        {
            var (manager, _) = Create();

            Assert.Equal(expected, manager.ComputeLayout(count));
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 4)]
        [InlineData(5, 4)]
        public void CreateWorkspace_ClampsPanelCount(int input, int expected)
        {
            var (manager, _) = Create();

            var ws = manager.CreateWorkspace(input);

            Assert.Equal(expected, ws.PanelCount);
        }

        [Fact]
        public void AddPanel_RefusesMoreThanFour()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace(4, "Test");

            manager.AddPanel(ws.Id, "svc", "acc");

            Assert.Equal(4, ws.Panels.Count);
        }

        [Fact]
        public void AddPanel_AddsAtEndByDefault()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace(2, "Test");
            ws.Panels.Clear();

            manager.AddPanel(ws.Id, "svc1", "acc1");
            manager.AddPanel(ws.Id, "svc2", "acc2");

            Assert.Equal(2, ws.Panels.Count);
            Assert.Equal("svc1", ws.Panels[0].ServiceId);
            Assert.Equal("svc2", ws.Panels[1].ServiceId);
        }

        [Fact]
        public void RemovePanel_RemovesAndUpdatesOrders()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace(3, "Test");
            var idToRemove = ws.Panels[1].Id;

            manager.RemovePanel(ws.Id, idToRemove);

            Assert.Equal(2, ws.Panels.Count);
            Assert.DoesNotContain(ws.Panels, p => p.Id == idToRemove);
            Assert.Equal(0, ws.Panels[0].Order);
            Assert.Equal(1, ws.Panels[1].Order);
        }

        [Fact]
        public void SetAllTargets_SetsBroadcastMode()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace(3, "Test");

            manager.SetAllTargets(ws.Id, true);
            Assert.Equal(MultiAIBroadcastMode.All, ws.BroadcastMode);
            Assert.All(ws.Panels, p => Assert.True(p.IsPromptTarget));

            manager.SetAllTargets(ws.Id, false);
            Assert.Equal(MultiAIBroadcastMode.Disabled, ws.BroadcastMode);
        }

        [Fact]
        public void TogglePanelTarget_TransitionsBroadcastMode()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace(2, "Test");

            manager.SetAllTargets(ws.Id, false);

            manager.TogglePanelTarget(ws.Id, ws.Panels[0].Id);
            Assert.Equal(MultiAIBroadcastMode.Selected, ws.BroadcastMode);

            manager.TogglePanelTarget(ws.Id, ws.Panels[1].Id);
            Assert.Equal(MultiAIBroadcastMode.All, ws.BroadcastMode);
        }

        [Fact]
        public void RemovePanelsForService_RemovesMatchingPanels()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace(4, "Test");
            ws.Panels[0].ServiceId = "chatgpt";
            ws.Panels[1].ServiceId = "claude";
            ws.Panels[2].ServiceId = "chatgpt";
            ws.Panels[3].ServiceId = "gemini";

            manager.RemovePanelsForService("chatgpt");

            Assert.Equal(2, ws.Panels.Count);
            Assert.DoesNotContain(ws.Panels, p => p.ServiceId == "chatgpt");
        }

        [Fact]
        public void RemovePanelsForService_PadsToMinimumTwo()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace(2, "Test");
            ws.Panels[0].ServiceId = "only";
            ws.Panels[1].ServiceId = "only";

            manager.RemovePanelsForService("only");

            Assert.Equal(2, ws.Panels.Count);
            Assert.All(ws.Panels, p => Assert.Equal(string.Empty, p.ServiceId));
        }

        [Fact]
        public void SetActivePanel_UpdatesActiveId()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace(3, "Test");
            var target = ws.Panels[1].Id;

            manager.SetActivePanel(ws.Id, target);

            Assert.Equal(target, ws.ActivePanelId);
        }

        [Fact]
        public void ToggleMaximized_TogglesFlag()
        {
            var (manager, _) = Create();
            var ws = manager.CreateWorkspace(3, "Test");
            var target = ws.Panels[0].Id;

            manager.ToggleMaximized(ws.Id, target);

            Assert.True(ws.Panels.First(p => p.Id == target).IsMaximized);
        }
    }
}