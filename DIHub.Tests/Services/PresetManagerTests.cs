using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIHub.Core.Models;
using DIHub.Core.Services;
using DIHub.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DIHub.Tests.Services
{
    public class PresetManagerTests
    {
        private static PresetManager Create() =>
            new(new FakeConfigurationStorage(), NullLogger<PresetManager>.Instance);

        [Fact]
        public void CreatePreset_AddsAndFiresEvent()
        {
            var manager = Create();
            var fired = false;
            manager.PresetsChanged += (s, e) => fired = true;

            var preset = manager.CreatePreset("Team A", new[]
            {
                new TargetRef { ServiceId = "s1", AccountId = "a1" }
            });

            Assert.Single(manager.Presets);
            Assert.True(fired);
            Assert.Single(preset.Targets);
        }

        [Fact]
        public void RenamePreset_UpdatesName()
        {
            var manager = Create();
            var preset = manager.CreatePreset("Old", Enumerable.Empty<TargetRef>());

            manager.RenamePreset(preset.Id, "New");

            Assert.Equal("New", manager.Presets[0].Name);
        }

        [Fact]
        public void DeletePreset_Removes()
        {
            var manager = Create();
            var preset = manager.CreatePreset("X", Enumerable.Empty<TargetRef>());

            manager.DeletePreset(preset.Id);

            Assert.Empty(manager.Presets);
        }

        [Fact]
        public void AddHistory_PrependsNewestFirst()
        {
            var manager = Create();

            manager.AddHistory("first", Enumerable.Empty<TargetRef>(), Enumerable.Empty<string>(), 1, 0);
            manager.AddHistory("second", Enumerable.Empty<TargetRef>(), Enumerable.Empty<string>(), 2, 0);

            Assert.Equal("second", manager.History[0].Text);
            Assert.Equal("first", manager.History[1].Text);
        }

        [Fact]
        public void AddHistory_EmptyText_Ignored()
        {
            var manager = Create();

            manager.AddHistory("", Enumerable.Empty<TargetRef>(), Enumerable.Empty<string>(), 0, 0);
            manager.AddHistory("   ", Enumerable.Empty<TargetRef>(), Enumerable.Empty<string>(), 0, 0);

            Assert.Empty(manager.History);
        }

        [Fact]
        public void AddHistory_LimitsTo50Entries()
        {
            var manager = Create();

            for (int i = 0; i < 55; i++)
                manager.AddHistory($"prompt-{i}", Enumerable.Empty<TargetRef>(), Enumerable.Empty<string>(), 1, 0);

            Assert.Equal(50, manager.History.Count);
            Assert.Equal("prompt-54", manager.History[0].Text);
        }

        [Fact]
        public void ClearHistory_RemovesAll()
        {
            var manager = Create();
            manager.AddHistory("a", Enumerable.Empty<TargetRef>(), Enumerable.Empty<string>(), 1, 0);

            manager.ClearHistory();

            Assert.Empty(manager.History);
        }
    }
}