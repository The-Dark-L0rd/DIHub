using System;
using System.Collections.Generic;
using DIHub.Core.Models;

namespace DIHub.Infrastructure.Extensions
{
    /// <summary>
    /// Built-in catalog used when no remote catalog is configured.
    /// Entries with a non-null DownloadUrl support one-click install.
    /// Entries without one fall back to the "install manually" flow.
    ///
    /// NOTE: Curated DownloadUrls point at GitHub Releases. GitHub never
    ///       deletes old releases, so version-specific URLs remain valid.
    ///       Periodic review is recommended, but failures degrade gracefully
    ///       (the user is told the download failed and can paste a new URL).
    /// </summary>
    public static class ExtensionCatalogDefaults
    {
        private const string CategoryPrivacy = "Privacy";
        private const string CategorySecurity = "Security";
        private const string CategoryProductivity = "Productivity";
        private const string CategoryDeveloper = "Developer";
        private const string CategoryAccessibility = "Accessibility";
        private const string CategoryDarkMode = "Dark Mode";
        private const string CategoryUtilities = "Utilities";
        private const string CategoryAI = "AI";

        public static IReadOnlyList<ExtensionCatalogItem> All { get; } = Build();

        private static List<ExtensionCatalogItem> Build() => new()
        {
            // ── Dark Reader ──
            // GitHub's `latest` alias works because the release asset is named
            // `darkreader-chrome.zip` (no version in the filename).
            new ExtensionCatalogItem
            {
                Id = "catalog.dark-reader",
                Name = "Dark Reader",
                Description = "Dark mode for every website. Reduces eye strain and helps you browse at night.",
                Version = "4.9.100",
                Author = "Dark Reader Ltd",
                Glyph = "\uE708",
                HomepageUrl = "https://darkreader.org/",
                DownloadUrl = "https://github.com/darkreader/darkreader/releases/latest/download/darkreader-chrome.zip",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryDarkMode, CategoryAccessibility },
                Tags = new[] { "dark", "theme", "night" },
                DeclaredPermissions = new[] { "storage", "activeTab" },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Global,
                UpdatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            },

            // ── uBlock Origin ──
            // Version-specific URL. GitHub keeps old releases forever.
            new ExtensionCatalogItem
            {
                Id = "catalog.ublock-origin",
                Name = "uBlock Origin",
                Description = "An efficient wide-spectrum content blocker. Blocks ads, trackers, and malware domains.",
                Version = "1.62.0",
                Author = "Raymond Hill",
                Glyph = "\uEA18",
                HomepageUrl = "https://github.com/gorhill/uBlock",
                DownloadUrl = "https://github.com/gorhill/uBlock/releases/download/1.62.0/uBlock0_1.62.0.chromium.zip",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryPrivacy, CategorySecurity },
                Tags = new[] { "ads", "trackers", "blocker" },
                DeclaredPermissions = new[]
                {
                    "storage", "tabs", "webRequest", "webRequestBlocking", "<all_urls>"
                },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Global,
                UpdatedAt = new DateTime(2025, 2, 20, 0, 0, 0, DateTimeKind.Utc)
            },

            // ── ClearURLs ──
            new ExtensionCatalogItem
            {
                Id = "catalog.clearurls",
                Name = "ClearURLs",
                Description = "Automatically removes tracking elements from URLs.",
                Version = "1.27.3",
                Author = "Kevin R.",
                Glyph = "\uE71B",
                HomepageUrl = "https://github.com/ClearURLs/Addon",
                DownloadUrl = "https://github.com/ClearURLs/Addon/releases/download/v1.27.3/ClearURLs-1.27.3.zip",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryPrivacy },
                Tags = new[] { "url", "tracking", "privacy" },
                DeclaredPermissions = new[] { "storage", "webRequest", "<all_urls>" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── Privacy Badger (no curated URL) ──
            new ExtensionCatalogItem
            {
                Id = "catalog.privacy-badger",
                Name = "Privacy Badger",
                Description = "Automatically learns to block invisible trackers.",
                Version = "2024.6.1",
                Author = "Electronic Frontier Foundation",
                Glyph = "\uEA18",
                HomepageUrl = "https://privacybadger.org/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryPrivacy },
                Tags = new[] { "tracking", "ads", "eff" },
                DeclaredPermissions = new[] { "storage", "tabs", "webRequest", "<all_urls>" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── Bitwarden (no curated URL) ──
            new ExtensionCatalogItem
            {
                Id = "catalog.bitwarden",
                Name = "Bitwarden Password Manager",
                Description = "Open-source password manager. Cross-device sync, autofill, secure notes.",
                Version = "2024.12.1",
                Author = "Bitwarden Inc.",
                Glyph = "\uE72E",
                HomepageUrl = "https://bitwarden.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategorySecurity, CategoryProductivity },
                Tags = new[] { "password", "vault", "autofill" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                SuggestedScope = ExtensionScope.Account,
                UpdatedAt = new DateTime(2024, 12, 15, 0, 0, 0, DateTimeKind.Utc)
            },

            // ── LanguageTool ──
            new ExtensionCatalogItem
            {
                Id = "catalog.languagetool",
                Name = "LanguageTool",
                Description = "Grammar, spelling, and style checker for many languages.",
                Version = "8.12.0",
                Author = "LanguageTooler GmbH",
                Glyph = "\uE8A5",
                HomepageUrl = "https://languagetool.org/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryProductivity, CategoryAI },
                Tags = new[] { "grammar", "writing", "ai" },
                DeclaredPermissions = new[] { "storage", "contextMenus", "activeTab" },
                SuggestedScope = ExtensionScope.Account
            },

            // ── Grammarly ──
            new ExtensionCatalogItem
            {
                Id = "catalog.grammarly",
                Name = "Grammarly",
                Description = "AI-powered writing assistant for grammar and clarity.",
                Version = "14.1150.0",
                Author = "Grammarly, Inc.",
                Glyph = "\uE8A5",
                HomepageUrl = "https://www.grammarly.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryProductivity, CategoryAI },
                Tags = new[] { "writing", "grammar", "ai" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                SuggestedScope = ExtensionScope.Account
            },

            // ── Wappalyzer ──
            new ExtensionCatalogItem
            {
                Id = "catalog.wappalyzer",
                Name = "Wappalyzer",
                Description = "Identify technologies used on websites. Useful for developers.",
                Version = "6.10.70",
                Author = "Wappalyzer",
                Glyph = "\uE943",
                HomepageUrl = "https://www.wappalyzer.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryDeveloper },
                Tags = new[] { "tech-stack", "detection", "dev" },
                DeclaredPermissions = new[] { "storage", "tabs", "webRequest" },
                SuggestedScope = ExtensionScope.Account
            },

            // ── JSON Viewer ──
            new ExtensionCatalogItem
            {
                Id = "catalog.json-viewer",
                Name = "JSON Viewer",
                Description = "Beautiful JSON rendering with syntax highlighting and tree navigation.",
                Version = "0.7.0",
                Author = "tulios",
                Glyph = "\uE943",
                HomepageUrl = "https://github.com/tulios/json-viewer",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryDeveloper },
                Tags = new[] { "json", "dev", "format" },
                DeclaredPermissions = new[] { "storage", "<all_urls>" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── Video Speed Controller ──
            new ExtensionCatalogItem
            {
                Id = "catalog.video-speed",
                Name = "Video Speed Controller",
                Description = "Speed up, slow down, and advance videos on any website.",
                Version = "0.7.3",
                Author = "Igor Kuznetsov",
                Glyph = "\uE768",
                HomepageUrl = "https://github.com/igrigorik/videospeed",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryUtilities },
                Tags = new[] { "video", "speed", "youtube" },
                DeclaredPermissions = new[] { "storage", "<all_urls>" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── Markdown Viewer ──
            new ExtensionCatalogItem
            {
                Id = "catalog.markdown-viewer",
                Name = "Markdown Viewer",
                Description = "Renders Markdown files (.md, .markdown) with a rich preview.",
                Version = "5.5",
                Author = "simovs",
                Glyph = "\uE8A5",
                HomepageUrl = "https://github.com/simov/markdown-viewer",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryUtilities, CategoryDeveloper },
                Tags = new[] { "markdown", "readme", "docs" },
                DeclaredPermissions = new[] { "storage", "webRequest", "<all_urls>" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── Momentum ──
            new ExtensionCatalogItem
            {
                Id = "catalog.momentum",
                Name = "Momentum",
                Description = "Personal dashboard with focus, weather, and daily inspiration.",
                Version = "2.24.0",
                Author = "Momentum Dash Inc.",
                Glyph = "\uE7C1",
                HomepageUrl = "https://momentumdash.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryProductivity },
                Tags = new[] { "dashboard", "focus", "newtab" },
                DeclaredPermissions = new[] { "storage", "tabs" },
                SuggestedScope = ExtensionScope.Global
            }
        };
    }
}