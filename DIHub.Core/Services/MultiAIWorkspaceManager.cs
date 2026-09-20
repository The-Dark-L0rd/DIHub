using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Core.Services
{
    public sealed class MultiAIWorkspaceManager : IMultiAIWorkspaceManager
    {
        private readonly IConfigurationStorage _storage;
        private readonly ILogger<MultiAIWorkspaceManager> _logger;
        private readonly List<MultiAIWorkspace> _workspaces = new();
        private string? _activeWorkspaceId;

        public IReadOnlyList<MultiAIWorkspace> Workspaces => _workspaces;

        public MultiAIWorkspace? ActiveWorkspace
        {
            get
            {
                if (_activeWorkspaceId is null) return _workspaces.FirstOrDefault();
                return _workspaces.FirstOrDefault(w => w.Id == _activeWorkspaceId)
                    ?? _workspaces.FirstOrDefault();
            }
        }

        public event EventHandler? WorkspacesChanged;
        public event EventHandler? ActiveWorkspaceChanged;

        public MultiAIWorkspaceManager(IConfigurationStorage storage, ILogger<MultiAIWorkspaceManager> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            try
            {
                var config = await _storage.LoadAsync(ct);
                _workspaces.Clear();

                if (config.MultiAIWorkspaces is not null && config.MultiAIWorkspaces.Count > 0)
                {
                    _workspaces.AddRange(config.MultiAIWorkspaces);
                    _activeWorkspaceId = config.ActiveMultiAIWorkspaceId ?? _workspaces[0].Id;
                }

                WorkspacesChanged?.Invoke(this, EventArgs.Empty);
                ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Multi-AI workspaces.");
                _workspaces.Clear();
                WorkspacesChanged?.Invoke(this, EventArgs.Empty);
                ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task SaveAsync(CancellationToken ct = default)
        {
            try
            {
                var config = await _storage.LoadAsync(ct);
                config.MultiAIWorkspaces = _workspaces.ToList();
                config.ActiveMultiAIWorkspaceId = _activeWorkspaceId;
                await _storage.SaveAsync(config, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Multi-AI workspaces.");
            }
        }

        // ─────────────────────────────────────────────
        //  Workspace CRUD
        // ─────────────────────────────────────────────

        public MultiAIWorkspace CreateWorkspace(int panelCount, string? name = null)
        {
            if (panelCount < 2) panelCount = 2;
            if (panelCount > 4) panelCount = 4;

            var ws = new MultiAIWorkspace
            {
                Name = string.IsNullOrWhiteSpace(name) ? $"Multi-AI {panelCount}" : name.Trim(),
                PanelCount = panelCount,
                LayoutMode = ComputeLayout(panelCount)
            };

            // Pre-create empty panels so the workspace has the expected shape immediately.
            for (int i = 0; i < panelCount; i++)
            {
                ws.Panels.Add(new MultiAIPanel
                {
                    Order = i,
                    IsPromptTarget = true
                });
            }

            _workspaces.Add(ws);
            _activeWorkspaceId = ws.Id;

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
            return ws;
        }

        public void DeleteWorkspace(string workspaceId)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            _workspaces.Remove(ws);
            if (_activeWorkspaceId == workspaceId)
                _activeWorkspaceId = _workspaces.FirstOrDefault()?.Id;

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void RenameWorkspace(string workspaceId, string newName)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            ws.Name = string.IsNullOrWhiteSpace(newName) ? ws.Name : newName.Trim();
            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void SetActiveWorkspace(string workspaceId)
        {
            if (_activeWorkspaceId == workspaceId) return;
            if (_workspaces.All(w => w.Id != workspaceId)) return;

            _activeWorkspaceId = workspaceId;
            ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        // ─────────────────────────────────────────────
        //  Panel management
        // ─────────────────────────────────────────────

        public void AddPanel(string workspaceId, string serviceId, string accountId, int? atIndex = null)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            if (ws.Panels.Count >= 4)
            {
                _logger.LogWarning("Multi-AI workspace already has 4 panels.");
                return;
            }

            var panel = new MultiAIPanel
            {
                ServiceId = serviceId,
                AccountId = accountId,
                Order = ws.Panels.Count,
                IsPromptTarget = true
            };

            if (atIndex is int idx && idx >= 0 && idx <= ws.Panels.Count)
                ws.Panels.Insert(idx, panel);
            else
                ws.Panels.Add(panel);

            UpdateOrders(ws);
            ws.PanelCount = ws.Panels.Count;
            ws.LayoutMode = ComputeLayout(ws.PanelCount);

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void RemovePanel(string workspaceId, string panelId)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            var panel = ws.Panels.FirstOrDefault(p => p.Id == panelId);
            if (panel is null) return;

            ws.Panels.Remove(panel);
            if (ws.ActivePanelId == panelId)
                ws.ActivePanelId = ws.Panels.FirstOrDefault()?.Id;

            UpdateOrders(ws);
            ws.PanelCount = Math.Max(ws.Panels.Count, 2);
            ws.LayoutMode = ComputeLayout(ws.Panels.Count > 0 ? ws.Panels.Count : 2);

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void UpdatePanel(MultiAIPanel panel)
        {
            foreach (var ws in _workspaces)
            {
                var existing = ws.Panels.FirstOrDefault(p => p.Id == panel.Id);
                if (existing is null) continue;

                existing.ServiceId = panel.ServiceId;
                existing.AccountId = panel.AccountId;
                existing.Order = panel.Order;
                existing.WidthRatio = panel.WidthRatio;
                existing.HeightRatio = panel.HeightRatio;
                existing.IsMaximized = panel.IsMaximized;
                existing.IsPromptTarget = panel.IsPromptTarget;
                existing.IsEnabled = panel.IsEnabled;
                existing.IsLocked = panel.IsLocked;

                _ = SaveAsync();
                WorkspacesChanged?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        public void ReorderPanels(string workspaceId, IEnumerable<string> orderedPanelIds)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            var map = orderedPanelIds.ToList();
            var sorted = new List<MultiAIPanel>();
            foreach (var id in map)
            {
                var panel = ws.Panels.FirstOrDefault(p => p.Id == id);
                if (panel is not null) sorted.Add(panel);
            }
            foreach (var p in ws.Panels)
                if (!sorted.Contains(p)) sorted.Add(p);

            ws.Panels.Clear();
            foreach (var p in sorted) ws.Panels.Add(p);
            UpdateOrders(ws);

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void SetActivePanel(string workspaceId, string? panelId)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;
            if (ws.ActivePanelId == panelId) return;

            ws.ActivePanelId = panelId;
            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void ToggleMaximized(string workspaceId, string panelId)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            foreach (var p in ws.Panels)
                p.IsMaximized = p.Id == panelId && !p.IsMaximized;

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        // ─────────────────────────────────────────────
        //  Broadcast / Targets
        // ─────────────────────────────────────────────

        public void SetBroadcastMode(string workspaceId, MultiAIBroadcastMode mode)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            ws.BroadcastMode = mode;

            if (mode == MultiAIBroadcastMode.All)
                foreach (var p in ws.Panels) p.IsPromptTarget = true;
            else if (mode == MultiAIBroadcastMode.Disabled)
                foreach (var p in ws.Panels) p.IsPromptTarget = false;

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void SetAllTargets(string workspaceId, bool enabled)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            foreach (var p in ws.Panels) p.IsPromptTarget = enabled;
            ws.BroadcastMode = enabled ? MultiAIBroadcastMode.All : MultiAIBroadcastMode.Disabled;

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void TogglePanelTarget(string workspaceId, string panelId, bool? enabled = null)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            var panel = ws.Panels.FirstOrDefault(p => p.Id == panelId);
            if (panel is null) return;

            panel.IsPromptTarget = enabled ?? !panel.IsPromptTarget;

            var anyOn = ws.Panels.Any(p => p.IsPromptTarget);
            var allOn = ws.Panels.All(p => p.IsPromptTarget);
            ws.BroadcastMode = !anyOn
                ? MultiAIBroadcastMode.Disabled
                : (allOn ? MultiAIBroadcastMode.All : MultiAIBroadcastMode.Selected);

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void SetKeepTargetsForNext(string workspaceId, bool keep)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws is null) return;

            ws.KeepTargetsForNext = keep;
            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        // ─────────────────────────────────────────────
        //  Service cleanup
        // ─────────────────────────────────────────────

        public void RemovePanelsForService(string serviceId)
        {
            var anyChanged = false;

            foreach (var ws in _workspaces)
            {
                var toRemove = ws.Panels.Where(p => p.ServiceId == serviceId).ToList();
                if (toRemove.Count == 0) continue;

                foreach (var p in toRemove)
                {
                    ws.Panels.Remove(p);

                    if (ws.ActivePanelId == p.Id)
                        ws.ActivePanelId = ws.Panels.FirstOrDefault()?.Id;
                }

                while (ws.Panels.Count < 2)
                {
                    ws.Panels.Add(new MultiAIPanel
                    {
                        Order = ws.Panels.Count,
                        IsPromptTarget = true
                    });
                }

                UpdateOrders(ws);
                ws.PanelCount = ws.Panels.Count;
                ws.LayoutMode = ComputeLayout(ws.PanelCount);
                anyChanged = true;
            }

            if (anyChanged)
            {
                WorkspacesChanged?.Invoke(this, EventArgs.Empty);
                _ = SaveAsync();
            }
        }

        // ─────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────

        public MultiAILayoutMode ComputeLayout(int panelCount) => panelCount switch
        {
            2 => MultiAILayoutMode.TwoByOne,
            3 => MultiAILayoutMode.ThreeTopTwo,
            4 => MultiAILayoutMode.FourGrid,
            _ => MultiAILayoutMode.TwoByOne
        };

        private static void UpdateOrders(MultiAIWorkspace ws)
        {
            for (int i = 0; i < ws.Panels.Count; i++)
                ws.Panels[i].Order = i;
        }
    }
}