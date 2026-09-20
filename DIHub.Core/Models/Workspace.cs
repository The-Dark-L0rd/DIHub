using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DIHub.Core.Models
{
    public class Workspace : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        private string _name = "New Workspace";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private string _icon = "\uE8F2";
        public string Icon
        {
            get => _icon;
            set { if (_icon != value) { _icon = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// List of open tab URLs, in order.
        /// </summary>
        public List<string> OpenTabs { get; set; } = new();

        /// <summary>
        /// URL of the currently active tab, or null.
        /// </summary>
        public string? ActiveTabUrl { get; set; }

        /// <summary>
        /// Optional list of AIService IDs pinned in this workspace.
        /// Empty means "show all enabled services".
        /// </summary>
        public List<string> PinnedServiceIds { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Workspace Clone() => new()
        {
            Id = Id,
            Name = Name,
            Icon = Icon,
            OpenTabs = new List<string>(OpenTabs),
            ActiveTabUrl = ActiveTabUrl,
            PinnedServiceIds = new List<string>(PinnedServiceIds)
        };
    }
}