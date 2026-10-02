using System;

namespace DIHub.Core.Models
{
    /// <summary>
    /// Portable backup of DI Hub configuration. Never contains browser
    /// cookies, sessions, or extension package files — only configuration
    /// and metadata.
    /// </summary>
    public sealed class BackupFile
    {
        public int Version { get; set; } = 1;
        public string App { get; set; } = "DI Hub";
        public string AppVersion { get; set; } = "3.12.0";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        // Optional sections — null when that part is not included.
        public AppConfiguration? Configuration { get; set; }
        public ExtensionConfigFile? Extensions { get; set; }
        public BackupSettingsSection? Settings { get; set; }

        /// <summary>Flat list of what this backup contains (for UI).</summary>
        public string[] IncludedSections { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Subset of AppSettings worth backing up.
    /// Excludes window position (device-specific).
    /// </summary>
    public sealed class BackupSettingsSection
    {
        public bool? RestorePreviousSession { get; set; }
        public bool? ConfirmOnCloseMultipleTabs { get; set; }
        public bool? RunInBackground { get; set; }

        public Interfaces.AppTheme? Theme { get; set; }
        public Interfaces.AccentColor? Accent { get; set; }
        public bool? SidebarExpandedByDefault { get; set; }
        public bool? AnimationsEnabled { get; set; }

        public Interfaces.OpenLinksBehavior? OpenLinks { get; set; }
        public Interfaces.ExternalBrowser? ExternalBrowser { get; set; }
        public Interfaces.SearchEngine? SearchEngine { get; set; }

        public int? MultiAIDefaultPanelCount { get; set; }
        public bool? MultiAIConfirmMultiSend { get; set; }
        public bool? MultiAIKeepTargetsForNext { get; set; }
        public bool? MultiAIRememberLastTargets { get; set; }
        public bool? MultiAIAutoFocusActivePanel { get; set; }
        public bool? MultiAIAutomaticDispatch { get; set; }
        public bool? MultiAIAllowProviderAutomation { get; set; }

        public bool? ExtensionsEnabled { get; set; }
        public bool? AllowLocalExtensions { get; set; }
        public bool? AllowRemoteExtensions { get; set; }
        public bool? ExtensionConfirmInstall { get; set; }
        public bool? ExtensionShowPermissionWarnings { get; set; }
        public bool? ExtensionSafeMode { get; set; }
    }
}