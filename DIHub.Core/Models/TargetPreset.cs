using System;
using System.Collections.Generic;

namespace DIHub.Core.Models
{
    /// <summary>
    /// A reusable set of (Service, Account) targets.
    /// </summary>
    public class TargetPreset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "Preset";
        public List<TargetRef> Targets { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TargetRef
    {
        public string ServiceId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
    }
}