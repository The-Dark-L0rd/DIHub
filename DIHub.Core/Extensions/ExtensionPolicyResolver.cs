using System.Collections.Generic;
using System.Linq;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;

namespace DIHub.Infrastructure.Extensions
{
    /// <summary>
    /// Deterministic precedence: Account > Service > Global > Default(off).
    /// If the same extension has multiple assignments, the most specific wins.
    /// </summary>
    public sealed class ExtensionPolicyResolver : IExtensionPolicyResolver
    {
        public ExtensionEffectiveState Resolve(
            string extensionId,
            string? serviceId,
            string? accountId,
            IReadOnlyList<ExtensionAssignment> allAssignments)
        {
            if (string.IsNullOrEmpty(extensionId) || allAssignments is null)
                return new ExtensionEffectiveState { ExtensionId = extensionId ?? string.Empty };

            // 1) Account-specific override.
            if (!string.IsNullOrEmpty(accountId))
            {
                var accountRule = allAssignments.FirstOrDefault(a =>
                    a.ExtensionId == extensionId &&
                    a.Scope == ExtensionScope.Account &&
                    a.AccountId == accountId);

                if (accountRule is not null)
                    return new ExtensionEffectiveState
                    {
                        ExtensionId = extensionId,
                        IsEnabled = accountRule.IsEnabled,
                        Source = ExtensionScope.Account
                    };
            }

            // 2) Service-specific.
            if (!string.IsNullOrEmpty(serviceId))
            {
                var serviceRule = allAssignments.FirstOrDefault(a =>
                    a.ExtensionId == extensionId &&
                    a.Scope == ExtensionScope.Service &&
                    a.ServiceId == serviceId);

                if (serviceRule is not null)
                    return new ExtensionEffectiveState
                    {
                        ExtensionId = extensionId,
                        IsEnabled = serviceRule.IsEnabled,
                        Source = ExtensionScope.Service
                    };
            }

            // 3) Global.
            var globalRule = allAssignments.FirstOrDefault(a =>
                a.ExtensionId == extensionId &&
                a.Scope == ExtensionScope.Global);

            if (globalRule is not null)
                return new ExtensionEffectiveState
                {
                    ExtensionId = extensionId,
                    IsEnabled = globalRule.IsEnabled,
                    Source = ExtensionScope.Global
                };

            // 4) No explicit setting → off.
            return new ExtensionEffectiveState
            {
                ExtensionId = extensionId,
                IsEnabled = false,
                Source = ExtensionScope.None
            };
        }

        public string DescribeSource(ExtensionScope scope) => scope switch
        {
            ExtensionScope.Account => "Account override",
            ExtensionScope.Service => "Inherited from AI service",
            ExtensionScope.Global => "Global policy",
            _ => "Default (off)"
        };
    }
}