using System;
using DIHub.Core.Models;

namespace DIHub.APP.Controls.Extensions
{
    public sealed class ExtensionActionEventArgs : EventArgs
    {
        public ExtensionActionKind Kind { get; init; }
        public ExtensionCatalogItem? CatalogItem { get; init; }
        public ExtensionInfo? InstalledExtension { get; init; }
    }
}