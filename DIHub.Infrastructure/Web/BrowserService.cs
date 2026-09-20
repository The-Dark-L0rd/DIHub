using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DIHub.Core.Interfaces;
using DIHub.Core.Security;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Web
{
    public sealed class BrowserService : IBrowserService
    {
        private readonly ILogger<BrowserService> _logger;

        public BrowserService(ILogger<BrowserService> logger)
        {
            _logger = logger;
        }

        public bool IsValidWebUrl(string url)
            => UrlPolicy.IsWebUrl(url);

        public void OpenInDefaultBrowser(string url)
        {
            if (!UrlPolicy.IsWebUrl(url))
            {
                _logger.LogWarning("Attempted to open invalid URL: {Url}",
                    LogRedactor.SafeUrl(url));
                return;
            }

            _logger.LogInformation("Opening in default browser: {Url}",
                LogRedactor.SafeUrl(url));

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to open URL: {Error}",
                    LogRedactor.SafeException(ex));
            }
        }

        public void OpenInBrowser(string url, ExternalBrowser browser)
        {
            if (!UrlPolicy.IsWebUrl(url))
            {
                _logger.LogWarning("Attempted to open invalid URL: {Url}",
                    LogRedactor.SafeUrl(url));
                return;
            }

            if (browser == ExternalBrowser.Default)
            {
                OpenInDefaultBrowser(url);
                return;
            }

            string? exePath = browser switch
            {
                ExternalBrowser.Chrome => FindExecutable("chrome.exe"),
                ExternalBrowser.Edge => FindExecutable("msedge.exe"),
                _ => null
            };

            if (exePath != null)
            {
                _logger.LogInformation("Opening in {Browser}: {Url}",
                    browser, LogRedactor.SafeUrl(url));

                try
                {
                    Process.Start(new ProcessStartInfo(exePath, url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to open in {Browser}: {Error}",
                        browser, LogRedactor.SafeException(ex));
                }
            }
            else
            {
                _logger.LogWarning("{Browser} not found. Falling back to default browser.", browser);
                OpenInDefaultBrowser(url);
            }
        }

        private static string? FindExecutable(string exeName)
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Google", "Chrome", "Application", exeName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Google", "Chrome", "Application", exeName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft", "Edge", "Application", exeName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", exeName),
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        public string BuildSearchUrl(string query, SearchEngine engine = SearchEngine.Google)
        {
            var q = Uri.EscapeDataString(query);
            return engine switch
            {
                SearchEngine.Google => $"https://www.google.com/search?q={q}",
                SearchEngine.Bing => $"https://www.bing.com/search?q={q}",
                SearchEngine.DuckDuckGo => $"https://duckduckgo.com/?q={q}",
                _ => $"https://www.google.com/search?q={q}"
            };
        }

        public string NormalizeUrlOrSearch(string input, SearchEngine engine = SearchEngine.Google)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            input = input.Trim();

            // 1) Already has a scheme → validate and use as-is.
            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return UrlPolicy.IsWebUrl(input) ? input : BuildSearchUrl(input, engine);

            // 2) Reject dangerous schemes explicitly.
            var colonIndex = input.IndexOf(':');
            if (colonIndex > 0 && colonIndex < 10)
            {
                var prefix = input.Substring(0, colonIndex).ToLowerInvariant();
                if (prefix is "javascript" or "data" or "file" or "vbscript" or "about" or "ftp")
                    return BuildSearchUrl(input, engine);
            }

            // 3) Localhost special case.
            if (input.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("127.0.0.1"))
                return "http://" + input;

            // 4) Looks like a domain.
            if (!input.Contains(' ') &&
                input.Contains('.') &&
                Uri.TryCreate("https://" + input, UriKind.Absolute, out _))
                return "https://" + input;

            // 5) Treat as a search query.
            return BuildSearchUrl(input, engine);
        }
    }
}