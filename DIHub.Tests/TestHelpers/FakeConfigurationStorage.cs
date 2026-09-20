using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;

namespace DIHub.Tests.TestHelpers
{
    /// <summary>
    /// In-memory implementation of IConfigurationStorage for unit tests.
    /// Runs synchronously to keep assertions simple.
    /// </summary>
    public sealed class FakeConfigurationStorage : IConfigurationStorage
    {
        public AppConfiguration Config { get; set; } = new();

        public int SaveCallCount { get; private set; }
        public int LoadCallCount { get; private set; }

        public Task<AppConfiguration> LoadAsync(CancellationToken ct = default)
        {
            LoadCallCount++;
            return Task.FromResult(Config);
        }

        public Task SaveAsync(AppConfiguration config, CancellationToken ct = default)
        {
            SaveCallCount++;
            Config = config;
            return Task.CompletedTask;
        }
    }
}