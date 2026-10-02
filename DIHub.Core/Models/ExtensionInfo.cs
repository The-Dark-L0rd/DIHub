using System;

namespace DIHub.Core.Models
{
    /// <summary>
    /// A managed extension package on disk. This is the "definition" —
    /// independent of which profiles it is assigned to.
    /// </summary>
    public sealed class ExtensionInfo
    {
        /// <summary>
        /// DI Hub-internal stable identifier. Prefer:
        ///   1. Manifest `key` (Chrome ID derivation)
        ///   2. SHA-256 of (name + version + author)
        ///   3. Fallback: new GUID
        /// Never identify by display name alone.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>WebView2-assigned extension ID (set after first profile install).</summary>
        public string? WebView2Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Version { get; set; } = "0.0.0";
        public string? Author { get; set; }
        public string? Homepage { get; set; }
        public int ManifestVersion { get; set; }

        /// <summary>Folder containing manifest.json for the installed version.</summary>
        public string InstallPath { get; set; } = string.Empty;

        public ExtensionSourceType Source { get; set; } = ExtensionSourceType.Unknown;
        public string? SourceUrl { get; set; }
        public string? Publisher { get; set; }

        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>Relative path to icon within InstallPath (from manifest).</summary>
        public string? IconRelativePath { get; set; }

        public ExtensionCompatibility Compatibility { get; set; } = ExtensionCompatibility.Unknown;

        // ── Cached manifest data for UI ──
        public string[] Permissions { get; set; } = Array.Empty<string>();
        public string[] HostPermissions { get; set; } = Array.Empty<string>();

        public ExtensionInfo Clone() => new()
        {
            Id = Id,
            WebView2Id = WebView2Id,
            Name = Name,
            Description = Description,
            Version = Version,
            Author = Author,
            Homepage = Homepage,
            ManifestVersion = ManifestVersion,
            InstallPath = InstallPath,
            Source = Source,
            SourceUrl = SourceUrl,
            Publisher = Publisher,
            InstalledAt = InstalledAt,
            LastUpdatedAt = LastUpdatedAt,
            IconRelativePath = IconRelativePath,
            Compatibility = Compatibility,
            Permissions = (string[])Permissions.Clone(),
            HostPermissions = (string[])HostPermissions.Clone()
        };
    }
}