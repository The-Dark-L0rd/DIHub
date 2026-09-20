using System;
using System.Text.RegularExpressions;

namespace DIHub.Core.Security
{
    /// <summary>
    /// Sanitizes strings before they reach the logger.
    ///
    /// WebView2 URLs frequently carry sensitive data in query strings
    /// (session tokens, OAuth codes, magic-link tokens). Never log them raw.
    /// </summary>
    public static class LogRedactor
    {
        private static readonly Regex QueryParamPattern =
            new(@"([?&][^=&#]+)=([^&#]+)", RegexOptions.Compiled);

        private static readonly Regex BearerPattern =
            new(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TokenLikePattern =
            new(@"\b[A-Za-z0-9_\-]{24,}\b", RegexOptions.Compiled);

        /// <summary>
        /// Returns a safe version of the URL: host + path are kept, query values are masked.
        /// Example:
        ///   https://chatgpt.com/c/abc?token=SECRET&amp;foo=bar
        /// becomes
        ///   https://chatgpt.com/c/abc?token=***&amp;foo=***
        /// </summary>
        public static string SafeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    return "<invalid-url>";

                var baseUrl = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : ":" + uri.Port)}{uri.AbsolutePath}";

                if (string.IsNullOrEmpty(uri.Query))
                    return baseUrl;

                var redacted = QueryParamPattern.Replace(uri.Query, "$1=***");
                return baseUrl + redacted;
            }
            catch
            {
                return "<invalid-url>";
            }
        }

        /// <summary>
        /// Scrubs bearer tokens and long token-like strings from arbitrary messages.
        /// </summary>
        public static string SafeMessage(string? message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;

            var result = BearerPattern.Replace(message, "Bearer ***");
            result = TokenLikePattern.Replace(result, "***");
            return result;
        }

        /// <summary>
        /// Safely formats exception details for logging.
        /// </summary>
        public static string SafeException(Exception? ex)
        {
            if (ex is null) return string.Empty;

            var msg = SafeMessage(ex.Message);
            return $"{ex.GetType().Name}: {msg}";
        }
    }
}