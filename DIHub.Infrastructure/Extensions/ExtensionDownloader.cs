using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Extensions
{
    public sealed class ExtensionDownloader : IExtensionDownloader
    {
        /// <summary>Safety cap for remote extension packages.</summary>
        public const long MaxDownloadBytes = 50L * 1024 * 1024; // 50 MB

        private static readonly HttpClient _http = CreateHttpClient();

        private readonly IExtensionStorageService _storage;
        private readonly ILogger<ExtensionDownloader> _logger;

        public ExtensionDownloader(
            IExtensionStorageService storage,
            ILogger<ExtensionDownloader> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> DownloadToFileAsync(
            Uri url,
            IProgress<DownloadProgress>? progress,
            CancellationToken ct = default)
        {
            if (url is null) throw new ArgumentNullException(nameof(url));

            if (!string.Equals(url.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only HTTPS URLs are supported.");

            var tempDir = _storage.GetTempRootPath();
            Directory.CreateDirectory(tempDir);

            var tempFile = Path.Combine(
                tempDir,
                "ext_" + Guid.NewGuid().ToString("N") + ".zip");

            try
            {
                _logger.LogInformation("Downloading extension from {Host}", url.Host);

                using var response = await _http.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                response.EnsureSuccessStatusCode();

                var total = response.Content.Headers.ContentLength ?? -1L;

                if (total > MaxDownloadBytes)
                    throw new InvalidOperationException(
                        $"Extension package exceeds the maximum size of " +
                        $"{MaxDownloadBytes / 1024 / 1024} MB.");

                await using var src = await response.Content.ReadAsStreamAsync(ct);
                await using var dst = File.Create(tempFile);

                var buffer = new byte[81920];
                long received = 0;
                int n;

                while ((n = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await dst.WriteAsync(buffer.AsMemory(0, n), ct);
                    received += n;

                    if (received > MaxDownloadBytes)
                        throw new InvalidOperationException(
                            "Extension package exceeded the maximum size while streaming.");

                    progress?.Report(new DownloadProgress
                    {
                        CurrentStage = "Downloading",
                        BytesReceived = received,
                        TotalBytes = total,
                        PercentComplete = total > 0 ? (double)received / total : 0.0
                    });
                }

                _logger.LogInformation(
                    "Download complete from {Host} ({Bytes} bytes)",
                    url.Host, received);

                return tempFile;
            }
            catch
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); }
                catch { /* best-effort cleanup */ }
                throw;
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(120)
            };

            // A descriptive User-Agent is required by GitHub API and many hosts.
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "DIHub/3.12 (+https://github.com/The-Dark-L0rd)");

            client.DefaultRequestHeaders.Accept.ParseAdd(
                "application/zip, application/octet-stream, */*");

            return client;
        }
    }
}