using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DIHub.Core.Models
{
    public class TabItem : INotifyPropertyChanged
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");

        public string? AIServiceId { get; set; }
        public string? AccountId { get; set; }

        private string _title = "New Tab";
        public string Title
        {
            get => _title;
            set { if (_title != value) { _title = value; OnPropertyChanged(); } }
        }

        private string _url = string.Empty;
        public string Url
        {
            get => _url;
            set { if (_url != value) { _url = value; OnPropertyChanged(); } }
        }

        private string _icon = "\uE774";
        public string Icon
        {
            get => _icon;
            set { if (_icon != value) { _icon = value; OnPropertyChanged(); } }
        }

        private string? _accountName;
        public string? AccountName
        {
            get => _accountName;
            set { if (_accountName != value) { _accountName = value; OnPropertyChanged(); } }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}