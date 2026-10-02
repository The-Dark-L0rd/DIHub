using System;
using System.IO;
using DIHub.Core.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DIHub.APP.Controls.Extensions
{
    /// <summary>
    /// Resolves a displayable image for an extension:
    ///   1. Manifest icon file on disk (InstallPath + IconRelativePath)
    ///   2. Returns null — the caller falls back to a FontIcon glyph.
    /// Never throws — always safe to call.
    /// </summary>
    internal static class ExtensionIconHelper
    {
        /// <summary>
        /// Returns an ImageSource for the extension's icon file if it exists
        /// and is inside the install folder, else null.
        /// </summary>
        public static ImageSource? TryGetIcon(ExtensionInfo ext)
        {
            if (ext is null) return null;
            if (string.IsNullOrWhiteSpace(ext.IconRelativePath)) return null;
            if (string.IsNullOrWhiteSpace(ext.InstallPath)) return null;

            try
            {
                if (!Directory.Exists(ext.InstallPath)) return null;

                var root = Path.GetFullPath(
                    ext.InstallPath.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar);

                var combined = Path.GetFullPath(Path.Combine(root, ext.IconRelativePath!));

                // Never read a file outside the install folder.
                if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    return null;

                if (!File.Exists(combined)) return null;

                var bmp = new BitmapImage
                {
                    UriSource = new Uri(combined)
                };
                return bmp;
            }
            catch
            {
                return null;
            }
        }
    }
}