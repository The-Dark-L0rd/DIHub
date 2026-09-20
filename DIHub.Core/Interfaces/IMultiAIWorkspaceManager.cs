using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    public interface IMultiAIWorkspaceManager
    {
        IReadOnlyList<MultiAIWorkspace> Workspaces { get; }
        MultiAIWorkspace? ActiveWorkspace { get; }

        Task LoadAsync(CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);

        MultiAIWorkspace CreateWorkspace(int panelCount, string? name = null);
        void DeleteWorkspace(string workspaceId);
        void RenameWorkspace(string workspaceId, string newName);
        void SetActiveWorkspace(string workspaceId);

        void AddPanel(string workspaceId, string serviceId, string accountId, int? atIndex = null);
        void RemovePanel(string workspaceId, string panelId);
        void UpdatePanel(MultiAIPanel panel);
        void ReorderPanels(string workspaceId, IEnumerable<string> orderedPanelIds);

        void SetActivePanel(string workspaceId, string? panelId);
        void ToggleMaximized(string workspaceId, string panelId);

        void SetBroadcastMode(string workspaceId, MultiAIBroadcastMode mode);
        void SetAllTargets(string workspaceId, bool enabled);
        void TogglePanelTarget(string workspaceId, string panelId, bool? enabled = null);
        void SetKeepTargetsForNext(string workspaceId, bool keep);

        MultiAILayoutMode ComputeLayout(int panelCount);

        /// <summary>
        /// Removes every panel (in every workspace) whose ServiceId matches.
        /// Called when the whole AI service is deleted.
        /// </summary>
        void RemovePanelsForService(string serviceId);

        event EventHandler? WorkspacesChanged;
        event EventHandler? ActiveWorkspaceChanged;
    }
}