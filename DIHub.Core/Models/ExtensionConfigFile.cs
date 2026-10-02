using System.Collections.Generic;

namespace DIHub.Core.Models
{
    /// <summary>
    /// Root of extensions.json — kept separate from config.json so extension
    /// data is never overwritten by unrelated configuration saves.
    /// </summary>
    public sealed class ExtensionConfigFile
    {
        public int Version { get; set; } = 1;

        public List<ExtensionInfo> Extensions { get; set; } = new();
        public List<ExtensionAssignment> Assignments { get; set; } = new();
    }
}