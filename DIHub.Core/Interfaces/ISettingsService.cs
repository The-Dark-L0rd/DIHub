using System;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    public interface ISettingsService
    {
        AppSettings Current { get; }

        Task LoadAsync(CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);

        void Update(Action<AppSettings> mutation);

        event EventHandler? SettingsChanged;
    }
}