using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    public interface IWorkspaceManager
    {
        IReadOnlyList<Workspace> Workspaces { get; }
        Workspace? ActiveWorkspace { get; }

        Task LoadAsync(CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);

        Workspace CreateWorkspace(string name, string icon = "\uE8F2");
        void DeleteWorkspace(string id);
        void RenameWorkspace(string id, string newName);
        void UpdateWorkspace(Workspace workspace);
        void SwitchWorkspace(string id);

        /// <summary>
        /// Saves the current tab list into the active workspace.
        /// </summary>
        void CaptureCurrentTabs(IEnumerable<string> urls, string? activeUrl);

        event EventHandler? WorkspacesChanged;
        event EventHandler? ActiveWorkspaceChanged;
    }
}