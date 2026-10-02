using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    public sealed class ExtensionValidationResult
    {
        public bool IsValid { get; init; }
        public string? ErrorMessage { get; init; }
        public ExtensionManifest? Manifest { get; init; }

        /// <summary>Suggested stable DI Hub-internal ID.</summary>
        public string? SuggestedId { get; init; }

        public static ExtensionValidationResult Ok(ExtensionManifest m, string id)
            => new() { IsValid = true, Manifest = m, SuggestedId = id };

        public static ExtensionValidationResult Fail(string message)
            => new() { IsValid = false, ErrorMessage = message };
    }

    public interface IExtensionValidator
    {
        /// <summary>
        /// Reads and validates manifest.json inside <paramref name="extensionFolder"/>.
        /// Never executes extension code.
        /// </summary>
        Task<ExtensionValidationResult> ValidateFolderAsync(
            string extensionFolder, CancellationToken ct = default);
    }
}