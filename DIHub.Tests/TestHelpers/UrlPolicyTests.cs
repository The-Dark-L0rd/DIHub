using DIHub.Core.Security;
using Xunit;

namespace DIHub.Tests.Security
{
    public class UrlPolicyTests
    {
        // ── IsWebUrl ──

        [Theory]
        [InlineData("https://example.com", true)]
        [InlineData("http://example.com", true)]
        [InlineData("https://chatgpt.com/c/abc?x=1", true)]
        [InlineData("javascript:alert(1)", false)]
        [InlineData("data:text/html,<h1>hi</h1>", false)]
        [InlineData("file:///etc/passwd", false)]
        [InlineData("ftp://example.com", false)]
        [InlineData("about:blank", false)]
        [InlineData("not a url", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsWebUrl_ReturnsExpected(string? url, bool expected)
        {
            Assert.Equal(expected, UrlPolicy.IsWebUrl(url));
        }

        // ── IsHttpsUrl ──

        [Theory]
        [InlineData("https://example.com", true)]
        [InlineData("http://example.com", false)]
        [InlineData("javascript:void(0)", false)]
        public void IsHttpsUrl_ReturnsExpected(string url, bool expected)
        {
            Assert.Equal(expected, UrlPolicy.IsHttpsUrl(url));
        }

        // ── TryValidateServiceUrl ──

        [Fact]
        public void TryValidateServiceUrl_EmptyUrl_ReturnsError()
        {
            var ok = UrlPolicy.TryValidateServiceUrl("", out var error);

            Assert.False(ok);
            Assert.Contains("required", error, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TryValidateServiceUrl_JavascriptScheme_IsRejected()
        {
            var ok = UrlPolicy.TryValidateServiceUrl("javascript:alert(1)", out var error);

            Assert.False(ok);
            Assert.Contains("http", error, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TryValidateServiceUrl_EmbeddedCredentials_IsRejected()
        {
            var ok = UrlPolicy.TryValidateServiceUrl("https://user:pass@evil.com", out var error);

            Assert.False(ok);
            Assert.Contains("credentials", error, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TryValidateServiceUrl_InvalidUrl_ReturnsError()
        {
            var ok = UrlPolicy.TryValidateServiceUrl("not a url", out var error);

            Assert.False(ok);
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("https://chatgpt.com/")]
        [InlineData("http://localhost:3000/")]
        public void TryValidateServiceUrl_ValidUrl_Passes(string url)
        {
            var ok = UrlPolicy.TryValidateServiceUrl(url, out var error);

            Assert.True(ok);
            Assert.Empty(error);
        }
    }
}