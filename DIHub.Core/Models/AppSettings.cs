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

        // ── Window ──
        public WindowStateModel WindowState { get; set; } = new();

        // ── Session ──
        /// <summary>
        /// URLs of the tabs that were open when the app was last closed.
        /// Used on startup to restore the previous session.
        /// </summary>
        public List<string> LastSessionTabUrls { get; set; } = new();

        /// <summary>
        /// ServiceId of each tab (parallel to LastSessionTabUrls).
        /// </summary>
        public List<string> LastSessionTabServiceIds { get; set; } = new();

        /// <summary>
        /// AccountId of each tab (parallel to LastSessionTabUrls).
        /// </summary>
        public List<string> LastSessionTabAccountIds { get; set; } = new();

        /// <summary>
        /// Index of the active tab at last shutdown (-1 if none).
        /// </summary>
        public int LastSessionActiveIndex { get; set; } = -1;
    }
}