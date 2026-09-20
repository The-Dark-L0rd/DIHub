using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    public interface IAIAccountManager
    {
        IReadOnlyList<AIAccount> AllAccounts { get; }

        IReadOnlyList<AIAccount> GetAccountsForService(string serviceId);
        AIAccount? GetAccount(string accountId);

        Task LoadAsync(CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);

        AIAccount AddAccount(string serviceId, string name, string? icon = null);
        void UpdateAccount(AIAccount account);
        void RemoveAccount(string accountId);
        void RenameAccount(string accountId, string newName);
        void SetDefaultAccount(string accountId);
        void ToggleFavorite(string accountId);
        void TogglePinned(string accountId);
        void ReorderAccounts(string serviceId, IEnumerable<string> orderedIds);
        void MarkUsed(string accountId);

        string GetProfilePath(string accountId);
        Task EnsureProfileAsync(string accountId, CancellationToken ct = default);
        Task ClearProfileAsync(string accountId, CancellationToken ct = default);
        Task DeleteAccountAsync(string accountId, CancellationToken ct = default);

        /// <summary>
        /// Deletes every profile folder that belongs to this service's accounts.
        /// Called when the whole service is being removed.
        /// </summary>
        Task DeleteAllProfilesForServiceAsync(string serviceId, CancellationToken ct = default);

        event EventHandler? AccountsChanged;
    }
}