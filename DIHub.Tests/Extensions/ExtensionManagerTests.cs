using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using DIHub.Infrastructure.Extensions;
using DIHub.Tests.Extensions.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DIHub.Tests.Extensions
{
    public sealed class ExtensionManagerTests
    {
        // ─────────────────────────────────────────────
        //  Test doubles
        // ─────────────────────────────────────────────

        private sealed class InMemoryStorage : IExtensionStorageService
        {
            private ExtensionConfigFile _config = new();
            public string PackageRoot { get; }
            public string TempRoot { get; }

            public InMemoryStorage(string packageRoot, string tempRoot)
            {
                PackageRoot = packageRoot;
                TempRoot = tempRoot;
            }

            public Task<ExtensionConfigFile> LoadAsync(CancellationToken ct = default)
                => Task.FromResult(_config);

            public Task SaveAsync(ExtensionConfigFile config, CancellationToken ct = default)
            {
                _config = new ExtensionConfigFile
                {
                    Version = config.Version,
                    Extensions = new List<ExtensionInfo>(config.Extensions),
                    Assignments = new List<ExtensionAssignment>(config.Assignments)
                };
                return Task.CompletedTask;
            }

            public string GetPackageRootPath() => PackageRoot;
            public string GetTempRootPath() => TempRoot;
        }

        private sealed class NoopDownloader : IExtensionDownloader
        {
            public Task<string> DownloadToFileAsync(
                Uri url,
                IProgress<DownloadProgress>? progress,
                CancellationToken ct = default)
                => throw new NotSupportedException();
        }

        private static (ExtensionManager manager, InMemoryStorage storage, TempFolder temp)
            CreateManager()
        {
            var temp = new TempFolder();
            var packageRoot = temp.CreateSubFolder("packages");
            var tempRoot = temp.CreateSubFolder("temp");

            var storage = new InMemoryStorage(packageRoot, tempRoot);

            var manager = new ExtensionManager(
                storage,
                new ExtensionValidator(NullLogger<ExtensionValidator>.Instance),
                new ExtensionPolicyResolver(),
                new NoopDownloader(),
                NullLogger<ExtensionManager>.Instance);

            return (manager, storage, temp);
        }

        private static string CreateExtensionFolder(
            TempFolder temp, string name, string version = "1.0.0")
        {
            var folder = temp.CreateSubFolder($"src_{name}_{Guid.NewGuid():N}");
            File.WriteAllText(Path.Combine(folder, "manifest.json"), $@"{{
                ""manifest_version"": 3,
                ""name"": ""{name}"",
                ""version"": ""{version}""
            }}");
            return folder;
        }

        // ─────────────────────────────────────────────
        //  Load
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Load_EmptyConfig_NoExtensions()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                Assert.Empty(manager.InstalledExtensions);
                Assert.Empty(manager.Assignments);
            }
        }

        // ─────────────────────────────────────────────
        //  Install from folder
        // ─────────────────────────────────────────────

        [Fact]
        public async Task InstallFromFolder_ValidExtension_Succeeds()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var src = CreateExtensionFolder(temp, "MyExt");
                var result = await manager.InstallFromFolderAsync(
                    src,
                    ExtensionSourceType.LocalFolder,
                    sourceUrl: null);

                Assert.True(result.Success);
                Assert.NotNull(result.Extension);
                Assert.Single(manager.InstalledExtensions);
                Assert.Equal("MyExt", result.Extension!.Name);
            }
        }

        [Fact]
        public async Task InstallFromFolder_InvalidExtension_Fails()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var bad = temp.CreateSubFolder("bad_ext");
                File.WriteAllText(Path.Combine(bad, "manifest.json"), "{ not valid json }");

                var result = await manager.InstallFromFolderAsync(
                    bad,
                    ExtensionSourceType.LocalFolder,
                    sourceUrl: null);

                Assert.False(result.Success);
                Assert.Empty(manager.InstalledExtensions);
            }
        }

        [Fact]
        public async Task InstallFromFolder_ReplacesExisting()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var src1 = CreateExtensionFolder(temp, "MyExt", "1.0.0");
                await manager.InstallFromFolderAsync(src1, ExtensionSourceType.LocalFolder, null);

                var src2 = CreateExtensionFolder(temp, "MyExt", "1.0.0");
                await manager.InstallFromFolderAsync(src2, ExtensionSourceType.LocalFolder, null);

                Assert.Single(manager.InstalledExtensions);
            }
        }

        // ─────────────────────────────────────────────
        //  Remove
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Remove_ExistingExtension_RemovesIt()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var src = CreateExtensionFolder(temp, "MyExt");
                var installed = await manager.InstallFromFolderAsync(
                    src, ExtensionSourceType.LocalFolder, null);

                Assert.True(installed.Success);

                var removed = await manager.RemoveAsync(installed.Extension!.Id);
                Assert.True(removed);
                Assert.Empty(manager.InstalledExtensions);
            }
        }

        [Fact]
        public async Task Remove_Nonexistent_ReturnsFalse()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var removed = await manager.RemoveAsync("does-not-exist");
                Assert.False(removed);
            }
        }

        [Fact]
        public async Task Remove_AlsoDeletesAssignments()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var src = CreateExtensionFolder(temp, "MyExt");
                var installed = await manager.InstallFromFolderAsync(
                    src, ExtensionSourceType.LocalFolder, null);
                var id = installed.Extension!.Id;

                await manager.SetAssignmentAsync(
                    id, ExtensionScope.Global, null, null, true);

                Assert.Single(manager.Assignments);

                await manager.RemoveAsync(id);

                Assert.Empty(manager.Assignments);
            }
        }

        // ─────────────────────────────────────────────
        //  Assignments
        // ─────────────────────────────────────────────

        [Fact]
        public async Task SetAssignment_Global_AddsAssignment()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var src = CreateExtensionFolder(temp, "MyExt");
                var installed = await manager.InstallFromFolderAsync(
                    src, ExtensionSourceType.LocalFolder, null);
                var id = installed.Extension!.Id;

                await manager.SetAssignmentAsync(
                    id, ExtensionScope.Global, null, null, true);

                Assert.Single(manager.Assignments);
                var a = manager.Assignments[0];
                Assert.Equal(ExtensionScope.Global, a.Scope);
                Assert.True(a.IsEnabled);
            }
        }

        [Fact]
        public async Task SetAssignment_SameKeyReplaced()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var src = CreateExtensionFolder(temp, "MyExt");
                var installed = await manager.InstallFromFolderAsync(
                    src, ExtensionSourceType.LocalFolder, null);
                var id = installed.Extension!.Id;

                // Set global ON, then set it OFF.
                await manager.SetAssignmentAsync(
                    id, ExtensionScope.Global, null, null, true);
                await manager.SetAssignmentAsync(
                    id, ExtensionScope.Global, null, null, false);

                // Should have replaced, not added a second entry.
                Assert.Single(manager.Assignments);
                Assert.False(manager.Assignments[0].IsEnabled);
            }
        }

        [Fact]
        public async Task SetAssignment_MultipleScopes_AllPersist()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var src = CreateExtensionFolder(temp, "MyExt");
                var installed = await manager.InstallFromFolderAsync(
                    src, ExtensionSourceType.LocalFolder, null);
                var id = installed.Extension!.Id;

                await manager.SetAssignmentAsync(id, ExtensionScope.Global, null, null, true);
                await manager.SetAssignmentAsync(id, ExtensionScope.Service, "chatgpt", null, false);
                await manager.SetAssignmentAsync(id, ExtensionScope.Account, "chatgpt", "acc1", true);

                Assert.Equal(3, manager.Assignments.Count);

                // Effective state via resolver.
                var state = manager.GetEffectiveState(id, "chatgpt", "acc1");
                Assert.True(state.IsEnabled);
                Assert.Equal(ExtensionScope.Account, state.Source);
            }
        }

        // ─────────────────────────────────────────────
        //  Persistence round-trip
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Install_ThenReload_ExtensionPersists()
        {
            var temp = new TempFolder();
            using (temp)
            {
                var packageRoot = temp.CreateSubFolder("packages");
                var tempRoot = temp.CreateSubFolder("temp");
                var storage = new InMemoryStorage(packageRoot, tempRoot);

                // ── Session 1: install ──
                var manager1 = new ExtensionManager(
                    storage,
                    new ExtensionValidator(NullLogger<ExtensionValidator>.Instance),
                    new ExtensionPolicyResolver(),
                    new NoopDownloader(),
                    NullLogger<ExtensionManager>.Instance);

                await manager1.LoadAsync();

                var src = CreateExtensionFolder(temp, "PersistedExt");
                var installed = await manager1.InstallFromFolderAsync(
                    src, ExtensionSourceType.LocalFolder, null);
                Assert.True(installed.Success);

                await manager1.SetAssignmentAsync(
                    installed.Extension!.Id, ExtensionScope.Global, null, null, true);

                // ── Session 2: reload with same storage ──
                var manager2 = new ExtensionManager(
                    storage,
                    new ExtensionValidator(NullLogger<ExtensionValidator>.Instance),
                    new ExtensionPolicyResolver(),
                    new NoopDownloader(),
                    NullLogger<ExtensionManager>.Instance);

                await manager2.LoadAsync();

                Assert.Single(manager2.InstalledExtensions);
                Assert.Single(manager2.Assignments);
                Assert.Equal("PersistedExt", manager2.InstalledExtensions[0].Name);
            }
        }

        // ─────────────────────────────────────────────
        //  Event
        // ─────────────────────────────────────────────

        [Fact]
        public async Task ExtensionsChanged_FiresOnInstall()
        {
            var (manager, _, temp) = CreateManager();
            using (temp)
            {
                await manager.LoadAsync();

                var fired = 0;
                manager.ExtensionsChanged += (_, __) => fired++;

                var src = CreateExtensionFolder(temp, "EventExt");
                await manager.InstallFromFolderAsync(
                    src, ExtensionSourceType.LocalFolder, null);

                Assert.True(fired > 0);
            }
        }
    }
}