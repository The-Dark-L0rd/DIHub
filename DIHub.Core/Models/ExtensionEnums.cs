namespace DIHub.Core.Models
{
    /// <summary>Where the extension came from.</summary>
    public enum ExtensionSourceType
    {
        Unknown = 0,
        Catalog = 1,       // DI Hub-managed remote catalog
        Official = 2,      // Well-known official source
        Community = 3,     // Community-maintained source
        LocalFolder = 4,   // User selected a folder
        LocalZip = 5       // User selected a ZIP
    }

    /// <summary>Priority scope for an extension assignment.</summary>
    public enum ExtensionScope
    {
        None = 0,       // No explicit setting
        Global = 1,     // Applies to all profiles
        Service = 2,    // Applies to all accounts of a service
        Account = 3     // Applies only to one account (highest priority)
    }

    /// <summary>Current lifecycle state of an extension package.</summary>
    public enum ExtensionInstallState
    {
        NotInstalled = 0,
        PendingInstall = 1,   // Queued; applied on next WebView2 creation
        Installed = 2,
        InstalledDisabled = 3,
        Failed = 4
    }

    /// <summary>Factual compatibility verdict (not a made-up score).</summary>
    public enum ExtensionCompatibility
    {
        Unknown = 0,
        Supported = 1,
        PartiallySupported = 2,
        Unsupported = 3
    }
}