using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    /// <summary>
    /// Central coordinator for the DI Hub extension platform.
    /// Pure domain — does NOT reference any WebView2 types.
    /// </summary>
    public interface IExtensionManager
    {
        // ── State ──
        IReadOnlyList<ExtensionInfo> InstalledExtensions { get; }
        IReadOnlyList<ExtensionAssignment> Assignments { get; }
        ExtensionInfo? GetExtension(string extensionId);

        // ── Bootstrap ──
        Task LoadAsync(CancellationToken ct = default);

        // ── Install ──
        Task<ExtensionInstallResult> InstallFromFolderAsync(
            string sourceFolder,
            ExtensionSourceType source,
            string? sourceUrl,
            CancellationToken ct = default);

        Task<ExtensionInstallResult> InstallFromZipAsync(
            string zipPath,
            CancellationToken ct = default);

        Task<ExtensionInstallResult> InstallFromUrlAsync(
            string url,
            string? expectedSha256,
            IProgress<DownloadProgress>? progress,
            CancellationToken ct = default);

        // ── Remove ──
        Task<bool> RemoveAsync(string extensionId, CancellationToken ct = default);

        // ── Assignments (Global / Service / Account) ──
        Task SetAssignmentAsync(
            string extensionId,
            ExtensionScope scope,
            string? serviceId,
            string? accountId,
            bool enabled,
            CancellationToken ct = default);

        /// <summary>
        /// Atomically replaces ALL assignments for this extension with the
        /// given set. Used by the multi-select Manage Scope dialog.
        /// </summary>
        Task ReplaceAssignmentsAsync(
            string extensionId,
            IEnumerable<ExtensionAssignment> newAssignments,
            CancellationToken ct = default);

        ExtensionEffectiveState GetEffectiveState(
            string extensionId,
            string? serviceId,
            string? accountId);

        event EventHandler? ExtensionsChanged;
    }
}