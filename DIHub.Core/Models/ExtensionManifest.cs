using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DIHub.Core.Models
{
    /// <summary>
    /// Lightweight view over a browser extension's manifest.json.
    /// Only the fields DI Hub actually needs are parsed.
    /// </summary>
    public sealed class ExtensionManifest
    {
        [JsonPropertyName("manifest_version")]
        public int ManifestVersion { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("homepage_url")]
        public string? HomepageUrl { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("permissions")]
        public List<string>? Permissions { get; set; }

        [JsonPropertyName("host_permissions")]
        public List<string>? HostPermissions { get; set; }

        [JsonPropertyName("optional_permissions")]
        public List<string>? OptionalPermissions { get; set; }

        [JsonPropertyName("optional_host_permissions")]
        public List<string>? OptionalHostPermissions { get; set; }

        [JsonPropertyName("background")]
        public ManifestBackground? Background { get; set; }

        [JsonPropertyName("content_scripts")]
        public List<ManifestContentScript>? ContentScripts { get; set; }

        [JsonPropertyName("action")]
        public ManifestAction? Action { get; set; }

        [JsonPropertyName("browser_action")]
        public ManifestAction? BrowserAction { get; set; }

        [JsonPropertyName("page_action")]
        public ManifestAction? PageAction { get; set; }

        [JsonPropertyName("options_page")]
        public string? OptionsPage { get; set; }

        [JsonPropertyName("options_ui")]
        public ManifestOptionsUi? OptionsUi { get; set; }

        [JsonPropertyName("icons")]
        public Dictionary<string, string>? Icons { get; set; }
    }

    public sealed class ManifestBackground
    {
        [JsonPropertyName("service_worker")]
        public string? ServiceWorker { get; set; }

        [JsonPropertyName("scripts")]
        public List<string>? Scripts { get; set; }

        [JsonPropertyName("page")]
        public string? Page { get; set; }

        [JsonPropertyName("persistent")]
        public bool? Persistent { get; set; }
    }

    public sealed class ManifestContentScript
    {
        [JsonPropertyName("matches")]
        public List<string>? Matches { get; set; }

        [JsonPropertyName("js")]
        public List<string>? Js { get; set; }

        [JsonPropertyName("css")]
        public List<string>? Css { get; set; }

        [JsonPropertyName("run_at")]
        public string? RunAt { get; set; }
    }

    public sealed class ManifestAction
    {
        [JsonPropertyName("default_title")]
        public string? DefaultTitle { get; set; }

        [JsonPropertyName("default_popup")]
        public string? DefaultPopup { get; set; }

        [JsonPropertyName("default_icon")]
        public object? DefaultIcon { get; set; }
    }

    public sealed class ManifestOptionsUi
    {
        [JsonPropertyName("page")]
        public string? Page { get; set; }

        [JsonPropertyName("open_in_tab")]
        public bool? OpenInTab { get; set; }
    }
}