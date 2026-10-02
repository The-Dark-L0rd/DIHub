using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    /// <summary>
    /// Persistence for extension metadata + package storage layout.
    /// Deliberately separate from IConfigurationStorage so extension saves
    /// never conflict with config.json writers.
    /// </summary>
    public interface IExtensionStorageService
    {
        /// <summary>Loads extensions.json (returns empty on missing file).</summary>
        Task<ExtensionConfigFile> LoadAsync(CancellationToken ct = default);

        /// <summary>Atomically writes extensions.json.</summary>
        Task SaveAsync(ExtensionConfigFile config, CancellationToken ct = default);

        /// <summary>
        /// Root directory where unpacked extension packages live.
        /// Typically: %LOCALAPPDATA%\DIHub\Extensions\
        /// </summary>
        string GetPackageRootPath();

        /// <summary>
        /// Directory for temporary ZIP extraction.
        /// Typically: %LOCALAPPDATA%\DIHub\Temp\Extensions\
        /// </summary>
        string GetTempRootPath();
    }
}