using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Extensions
{
    public sealed class ExtensionValidator : IExtensionValidator
    {
        private const int MaxManifestBytes = 512 * 1024; // 512 KB safety cap

        private readonly ILogger<ExtensionValidator> _logger;

        public ExtensionValidator(ILogger<ExtensionValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExtensionValidationResult> ValidateFolderAsync(
            string extensionFolder, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(extensionFolder))
                return ExtensionValidationResult.Fail("Extension folder path is empty.");

            if (!Directory.Exists(extensionFolder))
                return ExtensionValidationResult.Fail("Extension folder does not exist.");

            var manifestPath = Path.Combine(extensionFolder, "manifest.json");
            if (!File.Exists(manifestPath))
                return ExtensionValidationResult.Fail("manifest.json not found in extension folder.");

            FileInfo fi;
            try
            {
                fi = new FileInfo(manifestPath);
                if (fi.Length <= 0 || fi.Length > MaxManifestBytes)
                    return ExtensionValidationResult.Fail("manifest.json has an unreasonable size.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to stat manifest.json");
                return ExtensionValidationResult.Fail("Unable to read manifest.json.");
            }

            ExtensionManifest? manifest;
            try
            {
                await using var stream = File.OpenRead(manifestPath);
                manifest = await JsonSerializer.DeserializeAsync<ExtensionManifest>(
                    stream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    ct);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "manifest.json is not valid JSON");
                return ExtensionValidationResult.Fail("manifest.json is not valid JSON.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read manifest.json");
                return ExtensionValidationResult.Fail("Failed to read manifest.json.");
            }

            if (manifest is null)
                return ExtensionValidationResult.Fail("manifest.json is empty.");

            if (manifest.ManifestVersion != 2 && manifest.ManifestVersion != 3)
                return ExtensionValidationResult.Fail(
                    $"Unsupported manifest_version: {manifest.ManifestVersion}. Expected 2 or 3.");

            if (string.IsNullOrWhiteSpace(manifest.Name))
                return ExtensionValidationResult.Fail("manifest.json is missing a 'name' field.");

            if (string.IsNullOrWhiteSpace(manifest.Version))
                return ExtensionValidationResult.Fail("manifest.json is missing a 'version' field.");

            // Basic sanity on version (any dotted numeric form is fine)
            if (!IsPlausibleVersion(manifest.Version))
                return ExtensionValidationResult.Fail(
                    $"manifest.json has an invalid 'version' value: {manifest.Version}");

            var id = ComputeStableId(manifest);
            return ExtensionValidationResult.Ok(manifest, id);
        }

        /// <summary>
        /// Deterministic ID from manifest content. Prefer the Chrome-style `key`,
        /// otherwise hash name+version+author.
        /// </summary>
        private static string ComputeStableId(ExtensionManifest m)
        {
            if (!string.IsNullOrWhiteSpace(m.Key))
            {
                // Chrome derives the extension ID from `key`. We hash the key
                // itself so DI Hub has a stable, deterministic ID.
                using var sha = SHA256.Create();
                var keyBytes = Convert.FromBase64String(SafeBase64(m.Key!));
                var hash = sha.ComputeHash(keyBytes);
                return ToHexLower(hash, 16); // first 16 bytes -> 32 hex chars
            }

            using var sha2 = SHA256.Create();
            var payload = $"{m.Name}|{m.Version}|{m.Author ?? string.Empty}";
            var bytes = sha2.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return ToHexLower(bytes, 16);
        }

        private static string SafeBase64(string value)
        {
            // Be defensive: manifest `key` should be base64, but if it isn't
            // we fall back to using the raw string.
            try
            {
                _ = Convert.FromBase64String(value);
                return value;
            }
            catch
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            }
        }

        private static string ToHexLower(byte[] bytes, int count)
        {
            var sb = new StringBuilder(count * 2);
            for (int i = 0; i < count && i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }

        private static bool IsPlausibleVersion(string v)
        {
            // Accept 1, 1.0, 1.0.0, 1.0.0.0 — reject anything else obvious.
            var parts = v.Split('.');
            if (parts.Length == 0 || parts.Length > 4) return false;
            foreach (var p in parts)
            {
                if (p.Length == 0 || p.Length > 5) return false;
                foreach (var c in p)
                    if (c < '0' || c > '9') return false;
            }
            return true;
        }
    }
}