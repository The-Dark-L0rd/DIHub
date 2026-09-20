using System;

namespace DIHub.Core.Models
{
    public enum CommandCategory
    {
        AIService,
        Navigation,
        Appearance,
        Developer,
        Workspace
    }

    public class CommandItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Icon { get; set; } = "\uE774";
        public string CategoryLabel { get; set; } = "Commands";
        public CommandCategory Category { get; set; } = CommandCategory.Navigation;
        public string? Keywords { get; set; }
        public Action? Execute { get; set; }
    }
}