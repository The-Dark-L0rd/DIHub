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
    public sealed class WorkspaceManager : IWorkspaceManager
    {
        private readonly IConfigurationStorage _storage;
        private readonly ILogger<WorkspaceManager> _logger;
        private readonly List<Workspace> _workspaces = new();
        private string? _activeWorkspaceId;

        public IReadOnlyList<Workspace> Workspaces => _workspaces;

        public Workspace? ActiveWorkspace
        {
            get
            {
                if (_activeWorkspaceId is null && _workspaces.Count > 0)
                    return _workspaces[0];

                return _workspaces.FirstOrDefault(w => w.Id == _activeWorkspaceId)
                       ?? _workspaces.FirstOrDefault();
            }
        }

        public event EventHandler? WorkspacesChanged;
        public event EventHandler? ActiveWorkspaceChanged;

        public WorkspaceManager(IConfigurationStorage storage, ILogger<WorkspaceManager> logger)
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
                _workspaces.Clear();

                if (config.Workspaces.Count == 0)
                {
                    _workspaces.AddRange(CreateDefaults());
                    _activeWorkspaceId = _workspaces[0].Id;
                    await SaveAsync(ct);
                }
                else
                {
                    _workspaces.AddRange(config.Workspaces);
                    _activeWorkspaceId = config.ActiveWorkspaceId ?? _workspaces[0].Id;
                }

                WorkspacesChanged?.Invoke(this, EventArgs.Empty);
                ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load workspaces. Using defaults.");
                _workspaces.Clear();
                _workspaces.AddRange(CreateDefaults());
                _activeWorkspaceId = _workspaces[0].Id;
                WorkspacesChanged?.Invoke(this, EventArgs.Empty);
                ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task SaveAsync(CancellationToken ct = default)
        {
            try
            {
                var config = await _storage.LoadAsync(ct);
                config.Workspaces = _workspaces.ToList();
                config.ActiveWorkspaceId = _activeWorkspaceId;
                await _storage.SaveAsync(config, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save workspaces.");
            }
        }

        // ─────────────────────────────────────────────
        //  CRUD
        // ─────────────────────────────────────────────

        public Workspace CreateWorkspace(string name, string icon = "\uE8F2")
        {
            var ws = new Workspace
            {
                Name = string.IsNullOrWhiteSpace(name) ? "New Workspace" : name.Trim(),
                Icon = icon
            };

            _workspaces.Add(ws);

            // Auto-activate the new workspace.
            _activeWorkspaceId = ws.Id;

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
            return ws;
        }

        public void DeleteWorkspace(string id)
        {
            if (_workspaces.Count <= 1)
            {
                _logger.LogWarning("Cannot delete the last workspace.");
                return;
            }

            var ws = _workspaces.FirstOrDefault(w => w.Id == id);
            if (ws is null) return;

            _workspaces.Remove(ws);

            if (_activeWorkspaceId == id)
                _activeWorkspaceId = _workspaces[0].Id;

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void RenameWorkspace(string id, string newName)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == id);
            if (ws is null) return;

            ws.Name = string.IsNullOrWhiteSpace(newName) ? ws.Name : newName.Trim();
            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void UpdateWorkspace(Workspace workspace)
        {
            var existing = _workspaces.FirstOrDefault(w => w.Id == workspace.Id);
            if (existing is null) return;

            existing.Name = workspace.Name;
            existing.Icon = workspace.Icon;
            existing.OpenTabs = workspace.OpenTabs;
            existing.ActiveTabUrl = workspace.ActiveTabUrl;
            existing.PinnedServiceIds = workspace.PinnedServiceIds;

            WorkspacesChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void SwitchWorkspace(string id)
        {
            if (_activeWorkspaceId == id) return;
            if (_workspaces.All(w => w.Id != id)) return;

            _activeWorkspaceId = id;
            ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }

        public void CaptureCurrentTabs(IEnumerable<string> urls, string? activeUrl)
        {
            var ws = ActiveWorkspace;
            if (ws is null) return;

            ws.OpenTabs = urls.Where(u => !string.IsNullOrEmpty(u)).ToList();
            ws.ActiveTabUrl = activeUrl;
            _ = SaveAsync();
        }

        // ─────────────────────────────────────────────
        //  Defaults
        // ─────────────────────────────────────────────

        private static IEnumerable<Workspace> CreateDefaults()
        {
            return new List<Workspace>
            {
                new() { Name = "Default",  Icon = "\uE80F" },
                new() { Name = "Work",     Icon = "\uE821" },
                new() { Name = "Study",    Icon = "\uE7BE" },
                new() { Name = "Research", Icon = "\uE721" },
            };
        }
    }
}