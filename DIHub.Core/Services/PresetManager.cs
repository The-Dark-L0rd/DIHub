using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Core.Services
{
    public sealed class PresetManager : IPresetManager
    {
        private const int MaxHistory = 50;

        private readonly IConfigurationStorage _storage;
        private readonly ILogger<PresetManager> _logger;

        private readonly List<TargetPreset> _presets = new();
        private readonly List<PromptHistoryEntry> _history = new();

        public IReadOnlyList<TargetPreset> Presets => _presets;
        public IReadOnlyList<PromptHistoryEntry> History => _history;

        public event EventHandler? PresetsChanged;
        public event EventHandler? HistoryChanged;

        public PresetManager(IConfigurationStorage storage, ILogger<PresetManager> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ─────────────────────────────────────────────
        //  Load / Save
        // ─────────────────────────────────────────────

        public async Task LoadAsync(CancellationToken ct = default)
        {
            try
            {
                var config = await _storage.LoadAsync(ct);

                _presets.Clear();
                _history.Clear();

                if (config.TargetPresets is not null)
                    _presets.AddRange(config.TargetPresets);

                if (config.PromptHistory is not null)
                    _history.AddRange(config.PromptHistory);

                PresetsChanged?.Invoke(this, EventArgs.Empty);
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load presets/history.");
            }
        }

        public async Task SaveAsync(CancellationToken ct = default)
        {
            try
            {
                var config = await _storage.LoadAsync(ct);
                config.TargetPresets = _presets.ToList();
                config.PromptHistory = _history.ToList();
                await _storage.SaveAsync(config, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save presets/history.");
            }
        }

        // ─────────────────────────────────────────────
        //  Presets
        // ─────────────────────────────────────────────

        public TargetPreset CreatePreset(string name, IEnumerable<TargetRef> targets)
        {
            var preset = new TargetPreset
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Preset" : name.Trim(),
                Targets = targets.Select(t => new TargetRef
                {
                    ServiceId = t.ServiceId,
                    AccountId = t.AccountId
                }).ToList()
            };

            _presets.Add(preset);
            PresetsChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
            return preset;
        }

        public void RenamePreset(string presetId, string newName)
        {
            var p = _presets.FirstOrDefault(x => x.Id == presetId);
            if (p is null) return;

            p.Name = string.IsNullOrWhiteSpace(newName) ? p.Name : newName.Trim();
            PresetsChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void DeletePreset(string presetId)
        {
            var p = _presets.FirstOrDefault(x => x.Id == presetId);
            if (p is null) return;

            _presets.Remove(p);
            PresetsChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        // ─────────────────────────────────────────────
        //  History
        // ─────────────────────────────────────────────

        public void AddHistory(string text, IEnumerable<TargetRef> targets,
            IEnumerable<string> targetLabels, int sentCount, int failedCount)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var entry = new PromptHistoryEntry
            {
                Text = text.Trim(),
                TargetLabels = targetLabels.ToList(),
                Targets = targets.Select(t => new TargetRef
                {
                    ServiceId = t.ServiceId,
                    AccountId = t.AccountId
                }).ToList(),
                SentCount = sentCount,
                FailedCount = failedCount
            };

            _history.Insert(0, entry);

            while (_history.Count > MaxHistory)
                _history.RemoveAt(_history.Count - 1);

            HistoryChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void RemoveHistoryEntry(string entryId)
        {
            var e = _history.FirstOrDefault(x => x.Id == entryId);
            if (e is null) return;

            _history.Remove(e);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void ClearHistory()
        {
            _history.Clear();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }
    }
}