using System.Collections.Generic;

namespace DIHub.Core.Models
{
    public class AppSettings
    {
        // ── General ──
        public bool RestorePreviousSession { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public bool ConfirmOnCloseMultipleTabs { get; set; } = true;
        public bool RunInBackground { get; set; } = false;

        // ── Appearance ──
        public Interfaces.AppTheme Theme { get; set; } = Interfaces.AppTheme.Dark;
        public Interfaces.AccentColor Accent { get; set; } = Interfaces.AccentColor.Purple;
        public bool SidebarExpandedByDefault { get; set; } = true;
        public bool AnimationsEnabled { get; set; } = true;

        // ── Browser ──
        public Interfaces.OpenLinksBehavior OpenLinks { get; set; } = Interfaces.OpenLinksBehavior.NewTab;
        public Interfaces.ExternalBrowser ExternalBrowser { get; set; } = Interfaces.ExternalBrowser.Default;
        public Interfaces.SearchEngine SearchEngine { get; set; } = Interfaces.SearchEngine.Google;

        // ── Multi-AI ──
        public int MultiAIDefaultPanelCount { get; set; } = 2;
        public bool MultiAIConfirmMultiSend { get; set; } = true;
        public bool MultiAIKeepTargetsForNext { get; set; } = false;
        public bool MultiAIRememberLastTargets { get; set; } = true;
        public bool MultiAIAutoFocusActivePanel { get; set; } = true;
        public bool MultiAIAutomaticDispatch { get; set; } = true;
        public bool MultiAIAllowProviderAutomation { get; set; } = true;

        // ── Extensions ──
        /// <summary>
        /// Enables WebView2's browser extension subsystem.
        /// Takes effect on the next application start.
        /// </summary>
        public bool ExtensionsEnabled { get; set; } = true;

        /// <summary>Allow installing extensions from local folders or ZIPs.</summary>
        public bool AllowLocalExtensions { get; set; } = true;

        /// <summary>Allow installing extensions from remote HTTPS URLs.</summary>
        public bool AllowRemoteExtensions { get; set; } = true;

        /// <summary>Show a confirmation dialog before installing.</summary>
        public bool ExtensionConfirmInstall { get; set; } = true;

        /// <summary>Show a permission breakdown before installing.</summary>
        public bool ExtensionShowPermissionWarnings { get; set; } = true;

        /// <summary>
        /// Safe mode: extensions are not auto-applied to any profile.
        /// Useful for diagnosing a misbehaving extension.
        /// </summary>
        public bool ExtensionSafeMode { get; set; } = false;

        // ── Window ──
        public WindowStateModel WindowState { get; set; } = new();

        // ── Session ──
        public List<string> LastSessionTabUrls { get; set; } = new();
        public List<string> LastSessionTabServiceIds { get; set; } = new();
        public List<string> LastSessionTabAccountIds { get; set; } = new();
        public int LastSessionActiveIndex { get; set; } = -1;
    }
}