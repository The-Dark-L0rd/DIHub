using System.Collections.Generic;
using DIHub.Core.Models;
using DIHub.Infrastructure.Extensions;
using Xunit;

namespace DIHub.Tests.Extensions
{
    public sealed class ExtensionPolicyResolverTests
    {
        private static ExtensionAssignment Global(string extId, bool enabled) => new()
        {
            ExtensionId = extId,
            Scope = ExtensionScope.Global,
            IsEnabled = enabled
        };

        private static ExtensionAssignment Service(string extId, string serviceId, bool enabled) => new()
        {
            ExtensionId = extId,
            Scope = ExtensionScope.Service,
            ServiceId = serviceId,
            IsEnabled = enabled
        };

        private static ExtensionAssignment Account(string extId, string serviceId, string accountId, bool enabled) => new()
        {
            ExtensionId = extId,
            Scope = ExtensionScope.Account,
            ServiceId = serviceId,
            AccountId = accountId,
            IsEnabled = enabled
        };

        // ─────────────────────────────────────────────
        //  No assignments
        // ─────────────────────────────────────────────

        [Fact]
        public void Resolve_NoAssignments_DisabledByDefault()
        {
            var resolver = new ExtensionPolicyResolver();
            var state = resolver.Resolve("ext1", "chatgpt", "personal", new List<ExtensionAssignment>());

            Assert.False(state.IsEnabled);
            Assert.Equal(ExtensionScope.None, state.Source);
            Assert.True(state.IsDefault);
        }

        // ─────────────────────────────────────────────
        //  Global
        // ─────────────────────────────────────────────

        [Fact]
        public void Resolve_GlobalEnabled_AppliesEverywhere()
        {
            var resolver = new ExtensionPolicyResolver();
            var assignments = new List<ExtensionAssignment>
            {
                Global("ext1", true)
            };

            var a = resolver.Resolve("ext1", "chatgpt", "personal", assignments);
            var b = resolver.Resolve("ext1", "claude", "work", assignments);

            Assert.True(a.IsEnabled);
            Assert.True(b.IsEnabled);
            Assert.Equal(ExtensionScope.Global, a.Source);
        }

        // ─────────────────────────────────────────────
        //  Service
        // ─────────────────────────────────────────────

        [Fact]
        public void Resolve_ServiceOverridesGlobal()
        {
            var resolver = new ExtensionPolicyResolver();
            var assignments = new List<ExtensionAssignment>
            {
                Global("ext1", true),
                Service("ext1", "chatgpt", false)
            };

            var chatgpt = resolver.Resolve("ext1", "chatgpt", "personal", assignments);
            var claude = resolver.Resolve("ext1", "claude", "work", assignments);

            Assert.False(chatgpt.IsEnabled);
            Assert.Equal(ExtensionScope.Service, chatgpt.Source);

            Assert.True(claude.IsEnabled);
            Assert.Equal(ExtensionScope.Global, claude.Source);
        }

        // ─────────────────────────────────────────────
        //  Account
        // ─────────────────────────────────────────────

        [Fact]
        public void Resolve_AccountOverridesService()
        {
            var resolver = new ExtensionPolicyResolver();
            var assignments = new List<ExtensionAssignment>
            {
                Global("ext1", true),
                Service("ext1", "chatgpt", false),
                Account("ext1", "chatgpt", "work", true)
            };

            var personal = resolver.Resolve("ext1", "chatgpt", "personal", assignments);
            var work = resolver.Resolve("ext1", "chatgpt", "work", assignments);

            Assert.False(personal.IsEnabled);
            Assert.Equal(ExtensionScope.Service, personal.Source);

            Assert.True(work.IsEnabled);
            Assert.Equal(ExtensionScope.Account, work.Source);
        }

        [Fact]
        public void Resolve_AccountOverridesGlobal()
        {
            var resolver = new ExtensionPolicyResolver();
            var assignments = new List<ExtensionAssignment>
            {
                Global("ext1", false),
                Account("ext1", "chatgpt", "work", true)
            };

            var work = resolver.Resolve("ext1", "chatgpt", "work", assignments);
            Assert.True(work.IsEnabled);
            Assert.Equal(ExtensionScope.Account, work.Source);
        }

        // ─────────────────────────────────────────────
        //  No service / account context
        // ─────────────────────────────────────────────

        [Fact]
        public void Resolve_NoServiceContext_OnlyGlobalApplies()
        {
            var resolver = new ExtensionPolicyResolver();
            var assignments = new List<ExtensionAssignment>
            {
                Global("ext1", true),
                Service("ext1", "chatgpt", false)
            };

            var state = resolver.Resolve("ext1", null, null, assignments);
            Assert.True(state.IsEnabled);
            Assert.Equal(ExtensionScope.Global, state.Source);
        }

        // ─────────────────────────────────────────────
        //  Spec §87: 4-profile test
        //  Assignments:
        //    uBlock        → ChatGPT (service)
        //    Dark Reader   → Global
        //    Bitwarden     → Claude / Work (account)
        // ─────────────────────────────────────────────

        [Fact]
        public void Resolve_FourProfileScenario_MatchesSpec()
        {
            var resolver = new ExtensionPolicyResolver();

            var assignments = new List<ExtensionAssignment>
            {
                Service("ublock", "chatgpt", true),
                Global("dark-reader", true),
                Account("bitwarden", "claude", "claude-work", true)
            };

            // ChatGPT / Personal
            var a1 = resolver.Resolve("ublock", "chatgpt", "chatgpt-personal", assignments);
            var a2 = resolver.Resolve("dark-reader", "chatgpt", "chatgpt-personal", assignments);
            var a3 = resolver.Resolve("bitwarden", "chatgpt", "chatgpt-personal", assignments);
            Assert.True(a1.IsEnabled);
            Assert.True(a2.IsEnabled);
            Assert.False(a3.IsEnabled);

            // ChatGPT / University
            var b1 = resolver.Resolve("ublock", "chatgpt", "chatgpt-univ", assignments);
            var b2 = resolver.Resolve("dark-reader", "chatgpt", "chatgpt-univ", assignments);
            var b3 = resolver.Resolve("bitwarden", "chatgpt", "chatgpt-univ", assignments);
            Assert.True(b1.IsEnabled);
            Assert.True(b2.IsEnabled);
            Assert.False(b3.IsEnabled);

            // Claude / Work
            var c1 = resolver.Resolve("ublock", "claude", "claude-work", assignments);
            var c2 = resolver.Resolve("dark-reader", "claude", "claude-work", assignments);
            var c3 = resolver.Resolve("bitwarden", "claude", "claude-work", assignments);
            Assert.False(c1.IsEnabled);
            Assert.True(c2.IsEnabled);
            Assert.True(c3.IsEnabled);

            // Gemini / Personal
            var d1 = resolver.Resolve("ublock", "gemini", "gemini-personal", assignments);
            var d2 = resolver.Resolve("dark-reader", "gemini", "gemini-personal", assignments);
            var d3 = resolver.Resolve("bitwarden", "gemini", "gemini-personal", assignments);
            Assert.False(d1.IsEnabled);
            Assert.True(d2.IsEnabled);
            Assert.False(d3.IsEnabled);
        }

        // ─────────────────────────────────────────────
        //  Spec §86: override scenario
        // ─────────────────────────────────────────────

        [Fact]
        public void Resolve_PersonalOnly_ThenGlobal_ThenOverride()
        {
            var resolver = new ExtensionPolicyResolver();

            // Step 1: Personal ON only.
            var s1 = new List<ExtensionAssignment>
            {
                Account("dr", "chatgpt", "personal", true)
            };
            Assert.True(resolver.Resolve("dr", "chatgpt", "personal", s1).IsEnabled);
            Assert.False(resolver.Resolve("dr", "chatgpt", "univ", s1).IsEnabled);

            // Step 2: Enable globally.
            var s2 = new List<ExtensionAssignment>
            {
                Account("dr", "chatgpt", "personal", true),
                Global("dr", true)
            };
            Assert.True(resolver.Resolve("dr", "chatgpt", "personal", s2).IsEnabled);
            Assert.True(resolver.Resolve("dr", "chatgpt", "univ", s2).IsEnabled);

            // Step 3: Override University OFF.
            var s3 = new List<ExtensionAssignment>
            {
                Account("dr", "chatgpt", "personal", true),
                Account("dr", "chatgpt", "univ", false),
                Global("dr", true)
            };
            Assert.True(resolver.Resolve("dr", "chatgpt", "personal", s3).IsEnabled);
            Assert.False(resolver.Resolve("dr", "chatgpt", "univ", s3).IsEnabled);
        }
    }
}