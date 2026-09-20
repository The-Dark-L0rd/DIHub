using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    public interface IPresetManager
    {
        IReadOnlyList<TargetPreset> Presets { get; }
        IReadOnlyList<PromptHistoryEntry> History { get; }

        Task LoadAsync(CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);

        TargetPreset CreatePreset(string name, IEnumerable<TargetRef> targets);
        void RenamePreset(string presetId, string newName);
        void DeletePreset(string presetId);

        void AddHistory(string text, IEnumerable<TargetRef> targets,
            IEnumerable<string> targetLabels, int sentCount, int failedCount);
        void RemoveHistoryEntry(string entryId);
        void ClearHistory();

        event EventHandler? PresetsChanged;
        event EventHandler? HistoryChanged;
    }
}