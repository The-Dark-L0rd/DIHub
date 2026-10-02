using System.Collections.Generic;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    /// <summary>
    /// Single source of truth for "is this extension active on this profile?".
    /// Precedence: Account > Service > Global > Default(off).
    /// </summary>
    public interface IExtensionPolicyResolver
    {
        ExtensionEffectiveState Resolve(
            string extensionId,
            string? serviceId,
            string? accountId,
            IReadOnlyList<ExtensionAssignment> allAssignments);

        /// <summary>All profile keys the extension is enabled for (for UI badges).</summary>
        string DescribeSource(ExtensionScope scope);
    }
}