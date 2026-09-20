using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace DIHub.APP.Services
{
    public enum PromptDispatchStatus
    {
        Sent,
        InputFound,
        Unavailable,
        Failed,
        Cancelled
    }

    public sealed class PromptDispatchResult
    {
        public PromptDispatchStatus Status { get; init; }
        public string? Error { get; init; }
        public string ProviderName { get; init; } = "Unknown";
    }

    public sealed class PromptDispatcher
    {
        private readonly List<IPromptStrategy> _strategies;

        public PromptDispatcher()
        {
            _strategies = new List<IPromptStrategy>
            {
                new ChatGPTStrategy(),
                new ClaudeStrategy(),
                new GeminiStrategy(),
                new GenericPromptStrategy()
            };
        }

        public async Task<PromptDispatchResult> SendAsync(
            CoreWebView2 core,
            string url,
            string prompt,
            CancellationToken ct = default)
        {
            if (core is null)
            {
                return new PromptDispatchResult
                {
                    Status = PromptDispatchStatus.Unavailable,
                    Error = "WebView2 not initialized."
                };
            }

            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(url))
                           ?? _strategies.Last();

            try
            {
                // ── 1) Type the prompt ──
                var typeScript = strategy.BuildTypeScript(prompt);
                var typeRaw = await core.ExecuteScriptAsync(typeScript);

                if (ct.IsCancellationRequested)
                {
                    return new PromptDispatchResult
                    {
                        Status = PromptDispatchStatus.Cancelled,
                        ProviderName = strategy.Name
                    };
                }

                if (!IsOk(typeRaw))
                {
                    return new PromptDispatchResult
                    {
                        Status = PromptDispatchStatus.Unavailable,
                        ProviderName = strategy.Name,
                        Error = ExtractReason(typeRaw)
                    };
                }

                // ── 2) Brief pause so the site registers the input ──
                await Task.Delay(220, ct);

                if (ct.IsCancellationRequested)
                {
                    return new PromptDispatchResult
                    {
                        Status = PromptDispatchStatus.Cancelled,
                        ProviderName = strategy.Name
                    };
                }

                // ── 3) Trigger send ──
                var sendScript = strategy.BuildSendScript();
                var sendRaw = await core.ExecuteScriptAsync(sendScript);

                if (IsOk(sendRaw))
                {
                    return new PromptDispatchResult
                    {
                        Status = PromptDispatchStatus.Sent,
                        ProviderName = strategy.Name
                    };
                }

                return new PromptDispatchResult
                {
                    Status = PromptDispatchStatus.InputFound,
                    ProviderName = strategy.Name,
                    Error = ExtractReason(sendRaw)
                };
            }
            catch (OperationCanceledException)
            {
                return new PromptDispatchResult
                {
                    Status = PromptDispatchStatus.Cancelled,
                    ProviderName = strategy.Name
                };
            }
            catch (Exception ex)
            {
                return new PromptDispatchResult
                {
                    Status = PromptDispatchStatus.Failed,
                    ProviderName = strategy.Name,
                    Error = ex.Message
                };
            }
        }

        // ─────────────────────────────────────────────
        //  JSON helpers
        // ─────────────────────────────────────────────

        private static bool IsOk(string rawResult)
        {
            var inner = TryUnwrap(rawResult);
            if (inner is null) return false;

            try
            {
                using var doc = JsonDocument.Parse(inner);
                return doc.RootElement.TryGetProperty("ok", out var ok)
                       && ok.ValueKind == JsonValueKind.True;
            }
            catch
            {
                return false;
            }
        }

        private static string? ExtractReason(string rawResult)
        {
            var inner = TryUnwrap(rawResult);
            if (inner is null) return null;

            try
            {
                using var doc = JsonDocument.Parse(inner);
                if (doc.RootElement.TryGetProperty("reason", out var r))
                    return r.GetString();
                if (doc.RootElement.TryGetProperty("method", out var m))
                    return "method:" + m.GetString();
            }
            catch { }

            return null;
        }

        /// <summary>
        /// ExecuteScriptAsync returns the result as a JSON-encoded string.
        /// So "result" arrives as "\"...\"". We unwrap once to get the inner string.
        /// </summary>
        private static string? TryUnwrap(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "null") return null;

            try
            {
                var unwrapped = JsonSerializer.Deserialize<string>(raw);
                return unwrapped;
            }
            catch
            {
                // If it wasn't JSON-encoded (defensive), return raw.
                return raw;
            }
        }
    }
}