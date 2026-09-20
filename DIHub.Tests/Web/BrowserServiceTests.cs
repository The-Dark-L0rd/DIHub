using System;
using DIHub.Core.Interfaces;
using DIHub.Infrastructure.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DIHub.Tests.Web
{
    public class BrowserServiceTests
    {
        private static BrowserService Create() =>
            new(NullLogger<BrowserService>.Instance);

        [Fact]
        public void IsValidWebUrl_AcceptsHttpAndHttps()
        {
            var svc = Create();

            Assert.True(svc.IsValidWebUrl("https://example.com"));
            Assert.True(svc.IsValidWebUrl("http://example.com"));
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("data:text/html,<h1>hi</h1>")]
        [InlineData("file:///etc/passwd")]
        [InlineData("")]
        [InlineData("no")]
        public void IsValidWebUrl_RejectsDangerousOrInvalid(string url)
        {
            var svc = Create();

            Assert.False(svc.IsValidWebUrl(url));
        }

        [Fact]
        public void BuildSearchUrl_Google()
        {
            var svc = Create();

            var url = svc.BuildSearchUrl("hello world", SearchEngine.Google);

            Assert.Contains("google.com/search", url);
            Assert.Contains("hello%20world", url);
        }

        [Theory]
        [InlineData(SearchEngine.Google, "google.com")]
        [InlineData(SearchEngine.Bing, "bing.com")]
        [InlineData(SearchEngine.DuckDuckGo, "duckduckgo.com")]
        public void BuildSearchUrl_UsesCorrectEngine(SearchEngine engine, string expectedHost)
        {
            var svc = Create();

            var url = svc.BuildSearchUrl("test", engine);

            Assert.Contains(expectedHost, url);
        }

        // ── NormalizeUrlOrSearch ──

        [Fact]
        public void Normalize_ValidUrl_ReturnedAsIs()
        {
            var svc = Create();

            var result = svc.NormalizeUrlOrSearch("https://example.com/path");

            Assert.Equal("https://example.com/path", result);
        }

        [Fact]
        public void Normalize_Domain_PrependsHttps()
        {
            var svc = Create();

            var result = svc.NormalizeUrlOrSearch("example.com");

            Assert.Equal("https://example.com", result);
        }

        [Fact]
        public void Normalize_SearchQuery_UsesGoogle()
        {
            var svc = Create();

            var result = svc.NormalizeUrlOrSearch("hello world", SearchEngine.Google);

            Assert.StartsWith("https://www.google.com/search", result);
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("data:text/html,<h1>x</h1>")]
        [InlineData("file:///etc/passwd")]
        [InlineData("vbscript:MsgBox")]
        public void Normalize_DangerousScheme_FallsBackToSearch(string input)
        {
            var svc = Create();

            var result = svc.NormalizeUrlOrSearch(input, SearchEngine.Google);

            // 1) Result must be a Google search URL — never the original scheme.
            Assert.StartsWith("https://www.google.com/search", result);

            // 2) Result must parse as a valid HTTPS URL.
            Assert.True(Uri.TryCreate(result, UriKind.Absolute, out var uri));
            Assert.Equal("https", uri!.Scheme);
            Assert.Equal("www.google.com", uri.Host);

            // 3) The dangerous scheme must never appear as a navigable prefix.
            Assert.False(result.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase));
            Assert.False(result.StartsWith("data:", StringComparison.OrdinalIgnoreCase));
            Assert.False(result.StartsWith("file:", StringComparison.OrdinalIgnoreCase));
            Assert.False(result.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Normalize_Localhost_UsesHttp()
        {
            var svc = Create();

            var result = svc.NormalizeUrlOrSearch("localhost:3000");

            Assert.Equal("http://localhost:3000", result);
        }

        [Fact]
        public void Normalize_Empty_ReturnsEmpty()
        {
            var svc = Create();

            Assert.Equal(string.Empty, svc.NormalizeUrlOrSearch(""));
            Assert.Equal(string.Empty, svc.NormalizeUrlOrSearch("   "));
        }
    }
}