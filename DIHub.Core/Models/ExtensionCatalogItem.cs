using System;

namespace DIHub.Core.Models
{
    /// <summary>
    /// A catalog entry — may or may not be installed yet.
    /// The catalog can be backed by static JSON, a remote API, or nothing at all.
    /// </summary>
    public sealed class ExtensionCatalogItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Version { get; set; } = string.Empty;
        public string? Author { get; set; }

        /// <summary>Optional FontIcon glyph for the card. Used when no IconUrl is set.</summary>
        public string? Glyph { get; set; }

        /// <summary>Optional remote icon URL. Kept empty for MVP to avoid network calls.</summary>
        public string? IconUrl { get; set; }

        public string? HomepageUrl { get; set; }

        public ExtensionSourceType Source { get; set; } = ExtensionSourceType.Catalog;

        /// <summary>Direct download URL for the extension package (ZIP). Empty for MVP.</summary>
        public string? DownloadUrl { get; set; }

        /// <summary>SHA-256 hex digest of the expected package, if known.</summary>
        public string? Sha256 { get; set; }

        public string[] Categories { get; set; } = Array.Empty<string>();
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>Permissions declared by the catalog (may be empty until downloaded).</summary>
        public string[] DeclaredPermissions { get; set; } = Array.Empty<string>();

        public DateTime? UpdatedAt { get; set; }

        /// <summary>True if the catalog marks this as featured.</summary>
        public bool IsFeatured { get; set; }

        /// <summary>
        /// Suggested default scope when installing (a hint for the install dialog).
        /// </summary>
        public ExtensionScope SuggestedScope { get; set; } = ExtensionScope.Global;
    }
}