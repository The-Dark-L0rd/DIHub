using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Extensions
{
    public sealed class ExtensionManager : IExtensionManager
    {
        private readonly IExtensionStorageService _storage;
        private readonly IExtensionValidator _validator;
        private readonly IExtensionPolicyResolver _policy;
        private readonly IExtensionDownloader _downloader;
        private readonly ILogger<ExtensionManager> _logger;

        private readonly List<ExtensionInfo> _extensions = new();
        private readonly List<ExtensionAssignment> _assignments = new();
        private readonly SemaphoreSlim _gate = new(1, 1);

        public IReadOnlyList<ExtensionInfo> InstalledExtensions => _extensions;
        public IReadOnlyList<ExtensionAssignment> Assignments => _assignments;

        public event EventHandler? ExtensionsChanged;

        public ExtensionManager(
            IExtensionStorageService storage,
            IExtensionValidator validator,
            IExtensionPolicyResolver policy,
            IExtensionDownloader downloader,
            ILogger<ExtensionManager> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ─────────────────────────────────────────────
        //  Load
        // ─────────────────────────────────────────────

        public async Task LoadAsync(CancellationToken ct = default)
        {
            var cfg = await _storage.LoadAsync(ct);

            await _gate.WaitAsync(ct);
            try
            {
                _extensions.Clear();
                _extensions.AddRange(cfg.Extensions);

                _assignments.Clear();
                _assignments.AddRange(cfg.Assignments);
            }
            finally { _gate.Release(); }

            _logger.LogInformation("Loaded {Count} extensions, {Assign} assignments.",
                _extensions.Count, _assignments.Count);
        }

        // ─────────────────────────────────────────────
        //  Query
        // ─────────────────────────────────────────────

        public ExtensionInfo? GetExtension(string extensionId)
            => _extensions.FirstOrDefault(e => e.Id == extensionId);

        public ExtensionEffectiveState GetEffectiveState(
            string extensionId, string? serviceId, string? accountId)
            => _policy.Resolve(extensionId, serviceId, accountId, _assignments);

        // ─────────────────────────────────────────────
        //  Install from folder
        // ─────────────────────────────────────────────

        public async Task<ExtensionInstallResult> InstallFromFolderAsync(
            string sourceFolder,
            ExtensionSourceType source,
            string? sourceUrl,
            CancellationToken ct = default)
        {
            var validation = await _validator.ValidateFolderAsync(sourceFolder, ct);
            if (!validation.IsValid || validation.Manifest is null ||
                string.IsNullOrEmpty(validation.SuggestedId))
            {
                return ExtensionInstallResult.Fail(
                    validation.ErrorMessage ?? "Extension validation failed.");
            }

            var manifest = validation.Manifest;
            var extensionId = validation.SuggestedId!;

            string installPath;
            try
            {
                var packageRoot = _storage.GetPackageRootPath();
                installPath = Path.Combine(packageRoot, extensionId, manifest.Version!);

                if (Directory.Exists(installPath))
                    Directory.Delete(installPath, recursive: true);

                Directory.CreateDirectory(installPath);
                CopyDirectory(sourceFolder, installPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy extension to managed storage.");
                return ExtensionInstallResult.Fail(
                    "Failed to copy extension files to disk.");
            }

            var info = BuildExtensionInfo(
                extensionId, manifest, installPath, source, sourceUrl);

            await _gate.WaitAsync(ct);
            try
            {
                _extensions.RemoveAll(e => e.Id == extensionId);
                _extensions.Add(info);

                await SaveConfigLockedAsync(ct);
            }
            finally { _gate.Release(); }

            _logger.LogInformation("Extension installed: {Name} {Version} ({Id})",
                info.Name, info.Version, info.Id);

            ExtensionsChanged?.Invoke(this, EventArgs.Empty);
            return ExtensionInstallResult.Ok(info);
        }

        // ─────────────────────────────────────────────
        //  Install from ZIP
        // ─────────────────────────────────────────────

        public async Task<ExtensionInstallResult> InstallFromZipAsync(
            string zipPath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
                return ExtensionInstallResult.Fail("ZIP file not found.");

            var tempRoot = Path.Combine(
                _storage.GetTempRootPath(),
                Guid.NewGuid().ToString("N"));

            try
            {
                await SafeZipExtractor.ExtractAsync(zipPath, tempRoot, _logger, ct);

                var manifestFolder = FindManifestFolder(tempRoot);
                if (manifestFolder is null)
                    return ExtensionInstallResult.Fail(
                        "No manifest.json found inside the ZIP archive.");

                return await InstallFromFolderAsync(
                    manifestFolder,
                    ExtensionSourceType.LocalZip,
                    sourceUrl: null,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZIP installation failed.");
                return ExtensionInstallResult.Fail(
                    "Failed to extract or install the ZIP archive.");
            }
            finally
            {
                try { Directory.Delete(tempRoot, recursive: true); }
                catch { /* best-effort */ }
            }
        }

        // ─────────────────────────────────────────────
        //  Install from URL (download + SHA-256 + install)
        // ─────────────────────────────────────────────

        public async Task<ExtensionInstallResult> InstallFromUrlAsync(
            string url,
            string? expectedSha256,
            IProgress<DownloadProgress>? progress,
            CancellationToken ct = default)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return ExtensionInstallResult.Fail("Invalid URL.");

            if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                return ExtensionInstallResult.Fail("Only HTTPS URLs are supported.");

            string? tempFile = null;

            try
            {
                // 1) Download to a temp file.
                tempFile = await _downloader.DownloadToFileAsync(uri, progress, ct);

                // 2) Optional SHA-256 verification.
                if (!string.IsNullOrWhiteSpace(expectedSha256))
                {
                    progress?.Report(new DownloadProgress { CurrentStage = "Verifying" });

                    var actual = await ComputeSha256Async(tempFile, ct);
                    var expected = expectedSha256.Trim().ToLowerInvariant();

                    if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "SHA-256 mismatch for {Url}. Expected {Expected}, got {Actual}.",
                            uri, expected, actual);

                        return ExtensionInstallResult.Fail(
                            "SHA-256 checksum mismatch. " +
                            "The file may be corrupted or tampered with.");
                    }
                }

                // 3) Install via the existing ZIP pipeline.
                progress?.Report(new DownloadProgress { CurrentStage = "Installing" });
                var result = await InstallFromZipAsync(tempFile, ct);

                // 4) Record the source URL on success.
                if (result.Success && result.Extension is not null)
                {
                    await _gate.WaitAsync(ct);
                    try
                    {
                        var existing = _extensions.FirstOrDefault(e =>
                            e.Id == result.Extension.Id);
                        if (existing is not null)
                        {
                            existing.Source = ExtensionSourceType.Catalog;
                            existing.SourceUrl = uri.ToString();
                            await SaveConfigLockedAsync(ct);
                        }
                    }
                    finally { _gate.Release(); }
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                return ExtensionInstallResult.Fail("Download was cancelled.");
            }
            catch (System.Net.Http.HttpRequestException hre)
            {
                _logger.LogWarning(hre, "Network error while downloading extension.");
                return ExtensionInstallResult.Fail(
                    "Network error while downloading the extension. " +
                    "Please check the URL and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "URL installation failed.");
                return ExtensionInstallResult.Fail(ex.Message);
            }
            finally
            {
                if (tempFile is not null)
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }

        // ─────────────────────────────────────────────
        //  Remove
        // ─────────────────────────────────────────────

        public async Task<bool> RemoveAsync(string extensionId, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                var info = _extensions.FirstOrDefault(e => e.Id == extensionId);
                if (info is null) return false;

                _extensions.RemoveAll(e => e.Id == extensionId);
                _assignments.RemoveAll(a => a.ExtensionId == extensionId);

                await SaveConfigLockedAsync(ct);
            }
            finally { _gate.Release(); }

            try
            {
                var extFolder = Path.Combine(_storage.GetPackageRootPath(), extensionId);
                if (Directory.Exists(extFolder))
                    Directory.Delete(extFolder, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to delete extension files for {Id}", extensionId);
            }

            _logger.LogInformation("Extension removed: {Id}", extensionId);

            ExtensionsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        // ─────────────────────────────────────────────
        //  Assignments
        // ─────────────────────────────────────────────

        public async Task SetAssignmentAsync(
            string extensionId,
            ExtensionScope scope,
            string? serviceId,
            string? accountId,
            bool enabled,
            CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                _assignments.RemoveAll(a =>
                    a.ExtensionId == extensionId &&
                    a.Scope == scope &&
                    a.ServiceId == serviceId &&
                    a.AccountId == accountId);

                _assignments.Add(new ExtensionAssignment
                {
                    ExtensionId = extensionId,
                    Scope = scope,
                    ServiceId = serviceId,
                    AccountId = accountId,
                    IsEnabled = enabled
                });

                await SaveConfigLockedAsync(ct);
            }
            finally { _gate.Release(); }

            ExtensionsChanged?.Invoke(this, EventArgs.Empty);
        }

        // ─────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────

        private async Task SaveConfigLockedAsync(CancellationToken ct)
        {
            var cfg = new ExtensionConfigFile
            {
                Version = 1,
                Extensions = _extensions.ToList(),
                Assignments = _assignments.ToList()
            };
            await _storage.SaveAsync(cfg, ct);
        }

        private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
        {
            using var sha = SHA256.Create();
            await using var stream = File.OpenRead(path);
            var hash = await sha.ComputeHashAsync(stream, ct);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static void CopyDirectory(string source, string dest)
        {
            foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(source, dir);
                Directory.CreateDirectory(Path.Combine(dest, rel));
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(source, file);
                var target = Path.Combine(dest, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }

        private static string? FindManifestFolder(string root)
        {
            if (File.Exists(Path.Combine(root, "manifest.json"))) return root;

            foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
            {
                if (File.Exists(Path.Combine(dir, "manifest.json"))) return dir;
            }
            return null;
        }

        private static ExtensionInfo BuildExtensionInfo(
            string id,
            ExtensionManifest m,
            string installPath,
            ExtensionSourceType source,
            string? sourceUrl)
        {
            string? iconRel = null;
            if (m.Icons is not null && m.Icons.Count > 0)
            {
                var best = m.Icons
                    .OrderByDescending(kvp => ParseSizeSafe(kvp.Key))
                    .First();
                iconRel = best.Value;
            }
            else if (m.Action?.DefaultIcon is string s) iconRel = s;

            return new ExtensionInfo
            {
                Id = id,
                Name = m.Name ?? "Unnamed Extension",
                Description = m.Description,
                Version = m.Version ?? "0.0.0",
                Author = m.Author,
                Homepage = m.HomepageUrl,
                ManifestVersion = m.ManifestVersion,
                InstallPath = installPath,
                Source = source,
                SourceUrl = sourceUrl,
                IconRelativePath = iconRel,
                Permissions = (m.Permissions ?? new List<string>()).ToArray(),
                HostPermissions = (m.HostPermissions ?? new List<string>()).ToArray(),
                InstalledAt = DateTime.UtcNow
            };
        }

        private static int ParseSizeSafe(string key)
            => int.TryParse(key, out var v) ? v : 0;
    }
}