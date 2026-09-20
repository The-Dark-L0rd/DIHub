using System;
using System.Collections.Generic;

namespace DIHub.Core.Models
{
    /// <summary>
    /// Lightweight metadata about a prompt the user sent to Multi-AI panels.
    /// Does NOT store AI responses.
    /// </summary>
    public class PromptHistoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Human-readable labels like ["ChatGPT · Personal", "Claude · Work"].
        /// </summary>
        public List<string> TargetLabels { get; set; } = new();

        /// <summary>
        /// Structured targets so a preset can be built from this entry later.
        /// </summary>
        public List<TargetRef> Targets { get; set; } = new();

        public int SentCount { get; set; }
        public int FailedCount { get; set; }
    }
}