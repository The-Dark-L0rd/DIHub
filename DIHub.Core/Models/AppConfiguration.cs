using System.Collections.Generic;

namespace DIHub.Core.Models
{
    public class AppConfiguration
    {
        public int Version { get; set; } = 7;
        public List<AIService> Services { get; set; } = new();
        public List<Workspace> Workspaces { get; set; } = new();
        public string? ActiveWorkspaceId { get; set; }

        public List<MultiAIWorkspace> MultiAIWorkspaces { get; set; } = new();
        public string? ActiveMultiAIWorkspaceId { get; set; }

        public List<TargetPreset> TargetPresets { get; set; } = new();
        public List<PromptHistoryEntry> PromptHistory { get; set; } = new();
    }
}