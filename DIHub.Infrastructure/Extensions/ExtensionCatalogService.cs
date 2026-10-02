using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Extensions
{
    /// <summary>
    /// Local-first catalog service.
    /// MVP: reads from ExtensionCatalogDefaults.
    /// Future: can layer on caching, remote JSON, or a DI Hub hosted catalog
    ///        without changing IExtensionCatalogService.
    /// </summary>
    public sealed class ExtensionCatalogService : IExtensionCatalogService
    {
        private readonly ILogger<ExtensionCatalogService> _logger;

        // Snapshot once — avoids re-allocating on every call.
        private readonly IReadOnlyList<ExtensionCatalogItem> _all;

        public ExtensionCatalogService(ILogger<ExtensionCatalogService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _all = ExtensionCatalogDefaults.All;
        }

        public Task<bool> IsAvailableAsync(CancellationToken ct = default)
            => Task.FromResult(true); // local catalog is always "available"

        public Task<IReadOnlyList<ExtensionCatalogItem>> GetAllAsync(
            CancellationToken ct = default)
            => Task.FromResult(_all);

        public Task<IReadOnlyList<ExtensionCatalogItem>> GetFeaturedAsync(
            CancellationToken ct = default)
        {
            IReadOnlyList<ExtensionCatalogItem> result =
                _all.Where(e => e.IsFeatured).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<ExtensionCatalogItem>> SearchAsync(
            string query, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult(_all);

            var q = query.Trim();

            IReadOnlyList<ExtensionCatalogItem> result = _all
                .Where(e => Matches(e, q))
                .OrderByDescending(e => e.IsFeatured)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<string>> GetCategoriesAsync(
            CancellationToken ct = default)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in _all)
                foreach (var c in e.Categories)
                    if (!string.IsNullOrWhiteSpace(c))
                        set.Add(c);

            IReadOnlyList<string> result = set
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<ExtensionCatalogItem?> GetDetailsAsync(
            string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Task.FromResult<ExtensionCatalogItem?>(null);

            var item = _all.FirstOrDefault(e =>
                string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(item);
        }

        private static bool Matches(ExtensionCatalogItem e, string q)
        {
            if (Contains(e.Name, q)) return true;
            if (Contains(e.Description, q)) return true;
            if (Contains(e.Author, q)) return true;

            foreach (var c in e.Categories)
                if (Contains(c, q)) return true;

            foreach (var t in e.Tags)
                if (Contains(t, q)) return true;

            return false;
        }

        private static bool Contains(string? haystack, string needle)
            => !string.IsNullOrEmpty(haystack)
               && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}