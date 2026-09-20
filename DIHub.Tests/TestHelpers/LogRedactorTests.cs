using System;
using DIHub.Core.Security;
using Xunit;

namespace DIHub.Tests.Security
{
    public class LogRedactorTests
    {
        // ── SafeUrl ──

        [Fact]
        public void SafeUrl_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, LogRedactor.SafeUrl(null));
            Assert.Equal(string.Empty, LogRedactor.SafeUrl(""));
            Assert.Equal(string.Empty, LogRedactor.SafeUrl("   "));
        }

        [Fact]
        public void SafeUrl_InvalidUrl_ReturnsPlaceholder()
        {
            Assert.Equal("<invalid-url>", LogRedactor.SafeUrl("not a url"));
        }

        [Fact]
        public void SafeUrl_NoQuery_Unchanged()
        {
            var result = LogRedactor.SafeUrl("https://chatgpt.com/c/abc");

            Assert.Equal("https://chatgpt.com/c/abc", result);
        }

        [Fact]
        public void SafeUrl_WithQuery_MasksValues()
        {
            var result = LogRedactor.SafeUrl("https://example.com/path?token=SECRET&foo=bar");

            Assert.Equal("https://example.com/path?token=***&foo=***", result);
            Assert.DoesNotContain("SECRET", result);
            Assert.DoesNotContain("bar", result);
        }

        // ── SafeMessage ──

        [Fact]
        public void SafeMessage_PlainText_Unchanged()
        {
            var msg = "Hello world";

            Assert.Equal(msg, LogRedactor.SafeMessage(msg));
        }

        [Fact]
        public void SafeMessage_BearerToken_Masked()
        {
            var msg = "Sent Authorization: Bearer abc123def456xyz";

            var result = LogRedactor.SafeMessage(msg);

            Assert.Contains("Bearer ***", result);
            Assert.DoesNotContain("abc123def456xyz", result);
        }

        [Fact]
        public void SafeMessage_LongTokenLikeString_Masked()
        {
            var msg = "Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";

            var result = LogRedactor.SafeMessage(msg);

            Assert.Contains("***", result);
            Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", result);
        }

        // ── SafeException ──

        [Fact]
        public void SafeException_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, LogRedactor.SafeException(null));
        }

        [Fact]
        public void SafeException_ReturnsTypeAndMessage()
        {
            var ex = new InvalidOperationException("Something went wrong");

            var result = LogRedactor.SafeException(ex);

            Assert.Contains("InvalidOperationException", result);
            Assert.Contains("Something went wrong", result);
        }
    }
}