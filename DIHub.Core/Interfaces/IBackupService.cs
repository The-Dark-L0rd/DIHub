using System;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    /// <summary>
    /// Creates and restores portable backups of DI Hub configuration.
    /// Never touches browser profiles or session data.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>Writes a backup file (JSON) to disk.</summary>
        Task ExportAsync(
            string destinationPath,
            bool includeServices,
            bool includeExtensions,
            bool includeSettings,
            CancellationToken ct = default);

        /// <summary>Reads a backup file without applying it.</summary>
        Task<BackupFile?> ReadAsync(string sourcePath, CancellationToken ct = default);

        /// <summary>Applies a backup. Only sections marked as non-null are restored.</summary>
        Task ApplyAsync(
            BackupFile backup,
            CancellationToken ct = default);
    }
}