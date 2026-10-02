using System;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    /// <summary>
    /// Downloads an extension ZIP from a remote HTTPS URL to a temp file.
    /// Never executes or inspects the file — that is the caller's responsibility.
    /// </summary>
    public interface IExtensionDownloader
    {
        /// <summary>
        /// Downloads the URL to a new temp file and returns its full path.
        /// The caller is responsible for deleting the returned file.
        /// Throws on network errors, non-HTTPS URLs, or size limits.
        /// </summary>
        Task<string> DownloadToFileAsync(
            Uri url,
            IProgress<DownloadProgress>? progress,
            CancellationToken ct = default);
    }
}