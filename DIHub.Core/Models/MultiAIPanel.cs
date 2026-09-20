using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DIHub.Core.Models
{
    public class MultiAIPanel : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ServiceId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;

        private int _order;
        public int Order
        {
            get => _order;
            set { if (_order != value) { _order = value; OnPropertyChanged(); } }
        }

        private double _widthRatio = 1.0;
        public double WidthRatio
        {
            get => _widthRatio;
            set { if (_widthRatio != value) { _widthRatio = value; OnPropertyChanged(); } }
        }

        private double _heightRatio = 1.0;
        public double HeightRatio
        {
            get => _heightRatio;
            set { if (_heightRatio != value) { _heightRatio = value; OnPropertyChanged(); } }
        }

        private bool _isMaximized;
        public bool IsMaximized
        {
            get => _isMaximized;
            set { if (_isMaximized != value) { _isMaximized = value; OnPropertyChanged(); } }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
        }

        private bool _isPromptTarget = true;
        public bool IsPromptTarget
        {
            get => _isPromptTarget;
            set { if (_isPromptTarget != value) { _isPromptTarget = value; OnPropertyChanged(); } }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); } }
        }

        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set { if (_isLocked != value) { _isLocked = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MultiAIPanel Clone() => new()
        {
            Id = Id,
            ServiceId = ServiceId,
            AccountId = AccountId,
            Order = Order,
            WidthRatio = WidthRatio,
            HeightRatio = HeightRatio,
            IsMaximized = IsMaximized,
            IsActive = IsActive,
            IsPromptTarget = IsPromptTarget,
            IsEnabled = IsEnabled,
            IsLocked = IsLocked
        };
    }
}