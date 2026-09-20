using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    public interface IAIServiceManager
    {
        IReadOnlyList<AIService> Services { get; }

        Task LoadAsync(CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);

        AIService AddService(AIService service);
        void UpdateService(AIService service);
        void RemoveService(string id);
        void ToggleFavorite(string id);
        void ReorderServices(IEnumerable<string> orderedIds);
        AIService? FindService(string id);
        void ResetToDefaults();

        event EventHandler? ServicesChanged;
    }
}