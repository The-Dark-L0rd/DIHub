using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DIHub.Core.Interfaces;

namespace DIHub.Core.Models
{
    public class AIAccount : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ServiceId { get; set; } = string.Empty;

        private string _name = "Personal";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private string _icon = "\uE77B";
        public string Icon
        {
            get => _icon;
            set { if (_icon != value) { _icon = value; OnPropertyChanged(); } }
        }

        private AccentColor _accent = AccentColor.Purple;
        public AccentColor Accent
        {
            get => _accent;
            set { if (_accent != value) { _accent = value; OnPropertyChanged(); } }
        }

        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set { if (_enabled != value) { _enabled = value; OnPropertyChanged(); } }
        }

        private bool _favorite;
        public bool Favorite
        {
            get => _favorite;
            set { if (_favorite != value) { _favorite = value; OnPropertyChanged(); } }
        }

        private bool _pinned;
        public bool Pinned
        {
            get => _pinned;
            set { if (_pinned != value) { _pinned = value; OnPropertyChanged(); } }
        }

        private int _order;
        public int Order
        {
            get => _order;
            set { if (_order != value) { _order = value; OnPropertyChanged(); } }
        }

        private bool _isDefault;
        public bool IsDefault
        {
            get => _isDefault;
            set { if (_isDefault != value) { _isDefault = value; OnPropertyChanged(); } }
        }

        private bool _openInExternalBrowser;
        public bool OpenInExternalBrowser
        {
            get => _openInExternalBrowser;
            set { if (_openInExternalBrowser != value) { _openInExternalBrowser = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Stable identifier used to name the WebView2 UserDataFolder.
        /// Never changes after creation, even if the display name changes.
        /// </summary>
        public string ProfileId { get; set; } = Guid.NewGuid().ToString("N");

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public AIAccount Clone() => new()
        {
            Id = Id,
            ServiceId = ServiceId,
            Name = Name,
            Icon = Icon,
            Accent = Accent,
            Enabled = Enabled,
            Favorite = Favorite,
            Pinned = Pinned,
            Order = Order,
            IsDefault = IsDefault,
            OpenInExternalBrowser = OpenInExternalBrowser,
            ProfileId = ProfileId,
            CreatedAt = CreatedAt,
            LastUsedAt = LastUsedAt
        };
    }
}