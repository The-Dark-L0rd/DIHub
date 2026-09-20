using System;

namespace DIHub.Core.Security
{
    /// <summary>
    /// Centralized URL validation used by every user-facing URL entry point.
    /// </summary>
    public static class UrlPolicy
    {
        /// <summary>
        /// Only http and https are allowed.
        /// javascript:, data:, file:, ftp:, and custom schemes are rejected.
        /// </summary>
        public static bool IsWebUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

            return uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps;
        }

        /// <summary>
        /// Returns true for HTTPS only. Used when we want to prefer secure targets.
        /// </summary>
        public static bool IsHttpsUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttps;
        }

        /// <summary>
        /// Validates a user-provided URL for a new AI service.
        /// Returns false with a user-friendly error message if invalid.
        /// </summary>
        public static bool TryValidateServiceUrl(string? url, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(url))
            {
                error = "URL is required.";
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                error = "URL is not valid.";
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                error = "Only http:// and https:// URLs are allowed.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(uri.Host))
            {
                error = "URL must have a valid host.";
                return false;
            }

            // No embedded credentials — never accept http://user:pass@host
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                error = "URL must not contain embedded credentials.";
                return false;
            }

            return true;
        }
    }
}