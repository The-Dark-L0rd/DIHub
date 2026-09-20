using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DIHub.Core.Interfaces;

namespace DIHub.Core.Models
{
    public class AIService : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private string _url = string.Empty;
        public string Url
        {
            get => _url;
            set { if (_url != value) { _url = value; OnPropertyChanged(); } }
        }

        private string _icon = "\uE8F2";
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

        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set { if (_enabled != value) { _enabled = value; OnPropertyChanged(); } }
        }

        private int _order;
        public int Order
        {
            get => _order;
            set { if (_order != value) { _order = value; OnPropertyChanged(); } }
        }

        private bool _openInExternalBrowser;
        public bool OpenInExternalBrowser
        {
            get => _openInExternalBrowser;
            set { if (_openInExternalBrowser != value) { _openInExternalBrowser = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Observable list of accounts. ObservableCollection ensures UI updates live.
        /// </summary>
        public ObservableCollection<AIAccount> Accounts { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public AIService Clone() => new()
        {
            Id = Id,
            Name = Name,
            Url = Url,
            Icon = Icon,
            Accent = Accent,
            Favorite = Favorite,
            Pinned = Pinned,
            Enabled = Enabled,
            Order = Order,
            OpenInExternalBrowser = OpenInExternalBrowser,
            Accounts = new ObservableCollection<AIAccount>(Accounts.Select(a => a.Clone()))
        };
    }
}