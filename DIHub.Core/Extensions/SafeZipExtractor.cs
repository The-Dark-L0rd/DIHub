using System;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Extensions
{
    /// <summary>
    /// Extracts a ZIP into a target folder while preventing path-traversal
    /// attacks. This is mandatory for handling untrusted archives.
    /// </summary>
    public static class SafeZipExtractor
    {
        private const long MaxUncompressedBytes = 200L * 1024 * 1024;   // 200 MB cap
        private const int MaxEntryCount = 5000;

        public static async Task ExtractAsync(
            string zipPath,
            string targetFolder,
            ILogger logger,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(zipPath))
                throw new ArgumentException("zipPath is required.", nameof(zipPath));
            if (string.IsNullOrWhiteSpace(targetFolder))
                throw new ArgumentException("targetFolder is required.", nameof(targetFolder));

            if (!File.Exists(zipPath))
                throw new FileNotFoundException("ZIP archive not found.", zipPath);

            Directory.CreateDirectory(targetFolder);
            var normalizedRoot = Path.GetFullPath(
                targetFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar);

            using var archive = ZipFile.OpenRead(zipPath);

            if (archive.Entries.Count > MaxEntryCount)
                throw new InvalidDataException(
                    $"ZIP contains too many entries ({archive.Entries.Count}).");

            long totalBytes = 0;

            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();

                // Directories may end with '/'
                if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/"))
                {
                    var dirPath = ResolveSafePath(normalizedRoot, entry.FullName);
                    Directory.CreateDirectory(dirPath);
                    continue;
                }

                totalBytes += entry.Length;
                if (totalBytes > MaxUncompressedBytes)
                    throw new InvalidDataException(
                        "ZIP uncompressed size exceeds the safety limit.");

                var destination = ResolveSafePath(normalizedRoot, entry.FullName);

                var destDir = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                try
                {
                    await using var src = entry.Open();
                    await using var dst = File.Create(destination);
                    await src.CopyToAsync(dst, 81920, ct);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex,
                        "Failed to extract ZIP entry {Entry}", entry.FullName);
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns the full, normalized destination path, or throws SecurityException
        /// if the entry would escape the target root.
        /// </summary>
        private static string ResolveSafePath(string normalizedRoot, string entryFullName)
        {
            if (string.IsNullOrWhiteSpace(entryFullName))
                throw new SecurityException("Empty ZIP entry path.");

            // Reject absolute paths and drive-letter prefixes explicitly.
            if (Path.IsPathRooted(entryFullName))
                throw new SecurityException($"Absolute path in ZIP entry: {entryFullName}");

            var combined = Path.GetFullPath(Path.Combine(normalizedRoot, entryFullName));

            if (!combined.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new SecurityException(
                    $"Path traversal detected in ZIP entry: {entryFullName}");

            return combined;
        }
    }
}