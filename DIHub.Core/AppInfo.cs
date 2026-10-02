using System.Reflection;

namespace DIHub.Core
{
    /// <summary>
    /// Central version info. Reads from the executing assembly, which is
    /// populated by Directory.Build.props. To change the version in the
    /// future, edit Directory.Build.props only.
    /// </summary>
    public static class AppInfo
    {
        /// <summary>e.g. "4.10.0"</summary>
        public static string Version
        {
            get
            {
                try
                {
                    var asm = typeof(AppInfo).Assembly;
                    var info = asm
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                        ?.InformationalVersion;

                    if (!string.IsNullOrWhiteSpace(info))
                    {
                        // Strip +build metadata if present (e.g. "4.10.0+abc123")
                        var plus = info.IndexOf('+');
                        return plus >= 0 ? info.Substring(0, plus) : info;
                    }

                    var v = asm.GetName().Version;
                    if (v is not null)
                        return $"{v.Major}.{v.Minor}.{v.Build}";

                    return "0.0.0";
                }
                catch
                {
                    return "0.0.0";
                }
            }
        }

        /// <summary>e.g. "4.10.0.0" — the 4-part version for MSIX.</summary>
        public static string Version4Part
        {
            get
            {
                var parts = Version.Split('.');
                return parts.Length switch
                {
                    1 => $"{parts[0]}.0.0.0",
                    2 => $"{parts[0]}.{parts[1]}.0.0",
                    3 => $"{parts[0]}.{parts[1]}.{parts[2]}.0",
                    _ => Version
                };
            }
        }

        public const string ProductName = "DI Hub";
        public const string Company = "DARK L0RD";
        public const string Copyright = "© 2026 DARK L0RD. All rights reserved.";
        public const string RepositoryUrl = "https://github.com/The-Dark-L0rd/DIHub";
    }
}