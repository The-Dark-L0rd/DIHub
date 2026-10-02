using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    /// <summary>
    /// Read-only catalog of discoverable extensions.
    /// Local-first: MVP implementation reads from static defaults; future
    /// implementations may add caching, remote APIs, or curated repositories.
    /// </summary>
    public interface IExtensionCatalogService
    {
        /// <summary>True when a remote catalog is reachable. Always true for local.</summary>
        Task<bool> IsAvailableAsync(CancellationToken ct = default);

        /// <summary>All entries known to the catalog.</summary>
        Task<IReadOnlyList<ExtensionCatalogItem>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Featured entries (IsFeatured == true).</summary>
        Task<IReadOnlyList<ExtensionCatalogItem>> GetFeaturedAsync(CancellationToken ct = default);

        /// <summary>Full-text search over name, description, tags, author.</summary>
        Task<IReadOnlyList<ExtensionCatalogItem>> SearchAsync(
            string query, CancellationToken ct = default);

        /// <summary>Distinct category labels.</summary>
        Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);

        /// <summary>Single entry by id, or null.</summary>
        Task<ExtensionCatalogItem?> GetDetailsAsync(
            string id, CancellationToken ct = default);
    }
}