using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace DIHub.APP.Services
{
    /// <summary>
    /// WebView2-aware layer of the extension system.
    /// Lives in the APP project because it depends on Microsoft.Web.WebView2.Core.
    /// </summary>
    public interface IExtensionProfileApplier
    {
        /// <summary>Called by WebView2Host after CoreWebView2 is initialized.</summary>
        void RegisterProfile(string accountId, CoreWebView2Profile profile);

        /// <summary>Called by WebView2Host.Dispose().</summary>
        void UnregisterProfile(string accountId);

        /// <summary>
        /// Ensures all extensions that should be enabled for this profile
        /// are actually installed + enabled on its WebView2 profile.
        /// Safe to call multiple times.
        /// </summary>
        Task ApplyExtensionsToProfileAsync(string accountId, CancellationToken ct = default);

        /// <summary>Re-syncs every registered live profile (after config change).</summary>
        Task SyncAllLiveProfilesAsync(CancellationToken ct = default);
    }
}