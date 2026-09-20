using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DIHub.Core.Models
{
    public enum MultiAILayoutMode
    {
        TwoByOne,      // 2 panels, side by side
        ThreeTopTwo,   // 3 panels: 2 top, 1 bottom wide
        FourGrid       // 4 panels: 2x2
    }

    public enum MultiAIBroadcastMode
    {
        Disabled,
        Selected,
        All
    }

    public class MultiAIWorkspace : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        private string _name = "Multi-AI";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private int _panelCount = 2;
        public int PanelCount
        {
            get => _panelCount;
            set { if (_panelCount != value) { _panelCount = value; OnPropertyChanged(); } }
        }

        private MultiAILayoutMode _layoutMode = MultiAILayoutMode.TwoByOne;
        public MultiAILayoutMode LayoutMode
        {
            get => _layoutMode;
            set { if (_layoutMode != value) { _layoutMode = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<MultiAIPanel> Panels { get; set; } = new();

        private string? _activePanelId;
        public string? ActivePanelId
        {
            get => _activePanelId;
            set { if (_activePanelId != value) { _activePanelId = value; OnPropertyChanged(); } }
        }

        private MultiAIBroadcastMode _broadcastMode = MultiAIBroadcastMode.Selected;
        public MultiAIBroadcastMode BroadcastMode
        {
            get => _broadcastMode;
            set { if (_broadcastMode != value) { _broadcastMode = value; OnPropertyChanged(); } }
        }

        private bool _keepTargetsForNext;
        public bool KeepTargetsForNext
        {
            get => _keepTargetsForNext;
            set { if (_keepTargetsForNext != value) { _keepTargetsForNext = value; OnPropertyChanged(); } }
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MultiAIWorkspace Clone()
        {
            var copy = new MultiAIWorkspace
            {
                Id = Id,
                Name = Name,
                PanelCount = PanelCount,
                LayoutMode = LayoutMode,
                ActivePanelId = ActivePanelId,
                BroadcastMode = BroadcastMode,
                KeepTargetsForNext = KeepTargetsForNext,
                CreatedAt = CreatedAt,
                LastUsedAt = LastUsedAt,
                Panels = new ObservableCollection<MultiAIPanel>(Panels.Select(p => p.Clone()))
            };
            return copy;
        }
    }
}