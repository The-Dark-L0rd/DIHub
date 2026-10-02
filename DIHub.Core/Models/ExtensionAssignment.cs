using System;

namespace DIHub.Core.Models
{
    /// <summary>
    /// Binds an ExtensionInfo to a scope (Global / Service / Account).
    /// An extension may have multiple assignments with different scopes.
    /// Precedence: Account > Service > Global (see IExtensionPolicyResolver).
    /// </summary>
    public sealed class ExtensionAssignment
    {
        public string ExtensionId { get; set; } = string.Empty;

        public ExtensionScope Scope { get; set; }

        /// <summary>Set when Scope == Service.</summary>
        public string? ServiceId { get; set; }

        /// <summary>Set when Scope == Account.</summary>
        public string? AccountId { get; set; }

        public bool IsEnabled { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public ExtensionAssignment Clone() => new()
        {
            ExtensionId = ExtensionId,
            Scope = Scope,
            ServiceId = ServiceId,
            AccountId = AccountId,
            IsEnabled = IsEnabled,
            AssignedAt = AssignedAt
        };
    }

    /// <summary>The resolved effective state for one extension on one profile.</summary>
    public sealed class ExtensionEffectiveState
    {
        public string ExtensionId { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
        public ExtensionScope Source { get; init; } = ExtensionScope.None;

        /// <summary>True when no explicit assignment was found for any scope.</summary>
        public bool IsDefault => Source == ExtensionScope.None;
    }
}