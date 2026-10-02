using System;
using System.Collections.Generic;
using DIHub.Core.Models;

namespace DIHub.Infrastructure.Extensions
{
    /// <summary>
    /// Built-in catalog used when no remote catalog is configured.
    /// Entries with a non-null DownloadUrl support one-click install.
    /// Entries without one fall back to the "install manually" flow
    /// (user downloads ZIP from the official source and installs it
    /// via "Install → From ZIP archive…").
    ///
    /// NOTE: Direct download URLs point at GitHub Releases or official
    /// sources. Version-specific URLs remain valid indefinitely on GitHub.
    /// </summary>
    public static class ExtensionCatalogDefaults
    {
        // ── Category labels ──
        private const string CategoryEssential = "Essential";
        private const string CategoryAI = "AI";
        private const string CategoryWriting = "Writing";
        private const string CategoryResearch = "Research";
        private const string CategoryDeveloper = "Developer";
        private const string CategoryProductivity = "Productivity";
        private const string CategoryPDF = "PDF";
        private const string CategoryRTL = "RTL";
        private const string CategoryPrivacy = "Privacy";
        private const string CategorySecurity = "Security";
        private const string CategoryAccessibility = "Accessibility";
        private const string CategoryDarkMode = "Dark Mode";
        private const string CategoryUtilities = "Utilities";

        public static IReadOnlyList<ExtensionCatalogItem> All { get; } = Build();

        private static List<ExtensionCatalogItem> Build() => new()
        {
            // ═══════════════════════════════════════════════════════════════
            //  ⭐ ESSENTIAL
            // ═══════════════════════════════════════════════════════════════

            // ── uBlock Origin Lite ──
            new ExtensionCatalogItem
            {
                Id = "catalog.ublock-origin-lite",
                Name = "uBlock Origin Lite",
                Description = "An efficient, permission-less, MV3 content blocker. " +
                              "Blocks ads, trackers, and malware domains without " +
                              "broad host permissions.",
                Version = "1.62.0",
                Author = "Raymond Hill",
                Glyph = "\uEA18",
                HomepageUrl = "https://github.com/uBlockOrigin/uBOL-home",
                DownloadUrl = "https://github.com/uBlockOrigin/uBOL-home/releases/download/2024.12.2.22/uBOLite_2024.12.2.22.chromium.mv3.zip",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryEssential, CategoryPrivacy, CategorySecurity },
                Tags = new[] { "ads", "trackers", "blocker", "mv3" },
                DeclaredPermissions = new[] { "storage", "alarms", "declarativeNetRequest" },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Global
            },

            // ── Dark Reader ──
            new ExtensionCatalogItem
            {
                Id = "catalog.dark-reader",
                Name = "Dark Reader",
                Description = "Dark mode for every website. Reduces eye strain and " +
                              "helps you browse at night.",
                Version = "4.9.129",
                Author = "Dark Reader Ltd",
                Glyph = "\uE708",
                HomepageUrl = "https://darkreader.org/",
                DownloadUrl = "https://github.com/darkreader/darkreader/releases/latest/download/darkreader-chrome.zip",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryEssential, CategoryDarkMode, CategoryAccessibility },
                Tags = new[] { "dark", "theme", "night" },
                DeclaredPermissions = new[] { "storage", "activeTab" },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Global
            },

            // ── Bitwarden ──
            new ExtensionCatalogItem
            {
                Id = "catalog.bitwarden",
                Name = "Bitwarden Password Manager",
                Description = "Open-source password manager. Cross-device sync, " +
                              "autofill, secure notes, and passkey support.",
                Version = "2026.7.0",
                Author = "Bitwarden Inc.",
                Glyph = "\uE72E",
                HomepageUrl = "https://bitwarden.com/",
                DownloadUrl = "https://github.com/bitwarden/clients/releases/download/browser-v2026.7.0/dist-chrome-2026.7.0.zip",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryEssential, CategorySecurity, CategoryProductivity },
                Tags = new[] { "password", "vault", "autofill", "passkey" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Account
            },

            // ── Google Translate ──
            new ExtensionCatalogItem
            {
                Id = "catalog.google-translate",
                Name = "Google Translate",
                Description = "Translate web pages and selected text into over 100 languages.",
                Version = "2.0.3",
                Author = "Google LLC",
                Glyph = "\uE8C1",
                HomepageUrl = "https://chrome.google.com/webstore/detail/google-translate/aapbdbdomjkkjkaonfhkkikfgjllcleb",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryEssential, CategoryResearch },
                Tags = new[] { "translate", "language", "google" },
                DeclaredPermissions = new[] { "storage", "contextMenus", "tabs" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── Immersive Translate ──
            new ExtensionCatalogItem
            {
                Id = "catalog.immersive-translate",
                Name = "Immersive Translate",
                Description = "Bilingual web page translation. Supports PDF, EPUB, " +
                              "subtitles, and hover translation with over 20 AI engines.",
                Version = "1.29.7",
                Author = "Immersive Translate",
                Glyph = "\uE8C1",
                HomepageUrl = "https://immersivetranslate.com/",
                DownloadUrl = "https://github.com/immersive-translate/immersive-translate/releases/download/v1.29.7/chrome-immersive-translate-1.29.7.zip",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryEssential, CategoryResearch, CategoryWriting },
                Tags = new[] { "translate", "bilingual", "pdf", "ai" },
                DeclaredPermissions = new[] { "storage", "tabs", "contextMenus", "<all_urls>" },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Global
            },

            // ── OneTab ──
            new ExtensionCatalogItem
            {
                Id = "catalog.onetab",
                Name = "OneTab",
                Description = "Convert all your tabs into a list. Save up to 95% " +
                              "memory and reduce tab clutter.",
                Version = "1.87",
                Author = "OneTab Ltd",
                Glyph = "\uE8A9",
                HomepageUrl = "https://www.one-tab.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryEssential, CategoryProductivity, CategoryUtilities },
                Tags = new[] { "tabs", "memory", "productivity" },
                DeclaredPermissions = new[] { "storage", "tabs" },
                SuggestedScope = ExtensionScope.Global
            },

            // ═══════════════════════════════════════════════════════════════
            //  🤖 AI
            // ═══════════════════════════════════════════════════════════════

            // ── Sider ──
            new ExtensionCatalogItem
            {
                Id = "catalog.sider",
                Name = "Sider — AI Sidebar",
                Description = "AI sidebar that works on any page. Ask questions, " +
                              "summarize, translate, and write with GPT, Claude, " +
                              "Gemini, DeepSeek, and Grok.",
                Version = "5.4.0",
                Author = "Sider AI",
                Glyph = "\uE71D",
                HomepageUrl = "https://sider.ai/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryAI, CategoryProductivity },
                Tags = new[] { "ai", "sidebar", "chat", "gpt" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Account
            },

            // ── HARPA AI ──
            new ExtensionCatalogItem
            {
                Id = "catalog.harpa-ai",
                Name = "HARPA AI",
                Description = "Web automation with ChatGPT, Claude, Gemini, and Grok. " +
                              "Summarize, monitor, and extract data from any page.",
                Version = "9.2.1",
                Author = "HARPA AI",
                Glyph = "\uE945",
                HomepageUrl = "https://harpa.ai/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryAI, CategoryProductivity },
                Tags = new[] { "ai", "automation", "chatgpt" },
                DeclaredPermissions = new[] { "storage", "tabs", "scripting", "<all_urls>" },
                SuggestedScope = ExtensionScope.Account
            },

            // ── Merlin AI ──
            new ExtensionCatalogItem
            {
                Id = "catalog.merlin-ai",
                Name = "Merlin AI",
                Description = "AI assistant for research, summarizing, translating, " +
                              "and creating content on any website.",
                Version = "2.5.8",
                Author = "Merlin AI",
                Glyph = "\uE7C1",
                HomepageUrl = "https://www.getmerlin.in/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryAI, CategoryProductivity },
                Tags = new[] { "ai", "research", "summarize" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                SuggestedScope = ExtensionScope.Account
            },

            // ── Monica ──
            new ExtensionCatalogItem
            {
                Id = "catalog.monica",
                Name = "Monica — All-in-One AI Assistant",
                Description = "All-in-one AI assistant with chat, search, writing, " +
                              "translation, and image tools. Works in any tab.",
                Version = "9.0.22",
                Author = "Monica AI",
                Glyph = "\uE99A",
                HomepageUrl = "https://monica.im/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryAI, CategoryProductivity },
                Tags = new[] { "ai", "assistant", "chat", "writing" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Account
            },

            // ── MaxAI ──
            new ExtensionCatalogItem
            {
                Id = "catalog.maxai",
                Name = "MaxAI.me",
                Description = "Ask AI anything while you browse. Supports GPT, " +
                              "Gemini, Claude, and Grok with a unified chat interface.",
                Version = "8.37.1",
                Author = "MaxAI.me",
                Glyph = "\uE943",
                HomepageUrl = "https://www.maxai.me/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryAI, CategoryProductivity },
                Tags = new[] { "ai", "chat", "gpt", "gemini" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                SuggestedScope = ExtensionScope.Account
            },

            // ── AIPRM ──
            new ExtensionCatalogItem
            {
                Id = "catalog.aiprm",
                Name = "AIPRM for ChatGPT",
                Description = "Curated prompt templates for ChatGPT. Over 4,500 " +
                              "prompts for SEO, marketing, writing, and coding.",
                Version = "1.4.3.19",
                Author = "AIPRM",
                Glyph = "\uE734",
                HomepageUrl = "https://www.aiprm.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryAI, CategoryWriting },
                Tags = new[] { "ai", "prompt", "chatgpt", "templates" },
                DeclaredPermissions = new[] { "storage", "tabs" },
                SuggestedScope = ExtensionScope.Account
            },

            // ── ChatGPT ──
            new ExtensionCatalogItem
            {
                Id = "catalog.chatgpt-extension",
                Name = "ChatGPT",
                Description = "Access ChatGPT from any page. Ask questions, " +
                              "summarize, and generate content without leaving the tab.",
                Version = "1.2.27236.6274",
                Author = "OpenAI",
                Glyph = "\uE8F2",
                HomepageUrl = "https://chatgpt.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryAI },
                Tags = new[] { "ai", "chatgpt", "openai" },
                DeclaredPermissions = new[] { "storage", "tabs" },
                SuggestedScope = ExtensionScope.Account
            },

            // ═══════════════════════════════════════════════════════════════
            //  ✍️ WRITING
            // ═══════════════════════════════════════════════════════════════

            // ── Grammarly ──
            new ExtensionCatalogItem
            {
                Id = "catalog.grammarly",
                Name = "Grammarly",
                Description = "AI-powered writing assistant for grammar, spelling, " +
                              "clarity, and tone. Works across websites.",
                Version = "14.1267.0",
                Author = "Grammarly, Inc.",
                Glyph = "\uE8A5",
                HomepageUrl = "https://www.grammarly.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryWriting, CategoryAI },
                Tags = new[] { "writing", "grammar", "ai" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                SuggestedScope = ExtensionScope.Account
            },

            // ── LanguageTool ──
            new ExtensionCatalogItem
            {
                Id = "catalog.languagetool",
                Name = "LanguageTool",
                Description = "Grammar, spelling, and style checker for over 25 " +
                              "languages. Free and privacy-friendly.",
                Version = "6.8",
                Author = "LanguageTooler GmbH",
                Glyph = "\uE8A5",
                HomepageUrl = "https://languagetool.org/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryWriting, CategoryAI },
                Tags = new[] { "grammar", "writing", "language" },
                DeclaredPermissions = new[] { "storage", "contextMenus", "activeTab" },
                SuggestedScope = ExtensionScope.Account
            },

            // ═══════════════════════════════════════════════════════════════
            //  🔬 RESEARCH
            // ═══════════════════════════════════════════════════════════════

            // ── Immersive Translate (already listed in Essential) ──
            // ── Google Translate (already listed in Essential) ──

            // ═══════════════════════════════════════════════════════════════
            //  👨‍💻 DEVELOPER
            // ═══════════════════════════════════════════════════════════════

            // ── React Developer Tools ──
            new ExtensionCatalogItem
            {
                Id = "catalog.react-devtools",
                Name = "React Developer Tools",
                Description = "Adds React debugging tools to the browser DevTools. " +
                              "Inspect component trees, props, state, and hooks.",
                Version = "5.3.1",
                Author = "Meta",
                Glyph = "\uE943",
                HomepageUrl = "https://github.com/facebook/react/tree/main/packages/react-devtools-extensions",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryDeveloper },
                Tags = new[] { "react", "devtools", "debug" },
                DeclaredPermissions = new[] { "storage", "scripting", "<all_urls>" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── Wappalyzer ──
            new ExtensionCatalogItem
            {
                Id = "catalog.wappalyzer",
                Name = "Wappalyzer",
                Description = "Identify the technologies used on websites — " +
                              "frameworks, CMS, analytics, and more.",
                Version = "6.12.1",
                Author = "Wappalyzer",
                Glyph = "\uE774",
                HomepageUrl = "https://www.wappalyzer.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryDeveloper },
                Tags = new[] { "tech-stack", "detection", "dev" },
                DeclaredPermissions = new[] { "storage", "tabs", "webRequest" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── JSON Viewer Pro ──
            new ExtensionCatalogItem
            {
                Id = "catalog.json-viewer-pro",
                Name = "JSON Viewer Pro",
                Description = "Visualize JSON responses with tree view, syntax " +
                              "highlighting, and one-click download.",
                Version = "1.0.6",
                Author = "rbrahul",
                Glyph = "\uE943",
                HomepageUrl = "https://github.com/rbrahul/Awesome-JSON-Viewer",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryDeveloper },
                Tags = new[] { "json", "dev", "format", "viewer" },
                DeclaredPermissions = new[] { "storage", "<all_urls>" },
                SuggestedScope = ExtensionScope.Global
            },

            // ═══════════════════════════════════════════════════════════════
            //  📚 PRODUCTIVITY
            // ═══════════════════════════════════════════════════════════════

            // ── Todoist ──
            new ExtensionCatalogItem
            {
                Id = "catalog.todoist",
                Name = "Todoist for Chrome",
                Description = "Add tasks to Todoist from any page. Planner, " +
                              "calendar, and quick add in one extension.",
                Version = "12.21.4",
                Author = "Doist",
                Glyph = "\uE8A9",
                HomepageUrl = "https://todoist.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryProductivity },
                Tags = new[] { "todo", "tasks", "planner" },
                DeclaredPermissions = new[] { "storage", "tabs", "contextMenus" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── Notion Web Clipper ──
            new ExtensionCatalogItem
            {
                Id = "catalog.notion-web-clipper",
                Name = "Notion Web Clipper",
                Description = "Save any web page to Notion with one click. " +
                              "Organize web content in your workspace.",
                Version = "0.2.13",
                Author = "Notion Labs",
                Glyph = "\uE8A5",
                HomepageUrl = "https://www.notion.com/",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryProductivity, CategoryResearch },
                Tags = new[] { "notion", "clipper", "notes" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                SuggestedScope = ExtensionScope.Global
            },

            // ═══════════════════════════════════════════════════════════════
            //  📄 PDF
            // ═══════════════════════════════════════════════════════════════

            // ── Adobe Acrobat ──
            new ExtensionCatalogItem
            {
                Id = "catalog.adobe-acrobat",
                Name = "Adobe Acrobat: PDF Tools",
                Description = "View, edit, convert, sign, and compress PDF files " +
                              "directly from the browser.",
                Version = "126.4.1.1",
                Author = "Adobe Inc.",
                Glyph = "\uEA90",
                HomepageUrl = "https://www.adobe.com/acrobat.html",
                Source = ExtensionSourceType.Official,
                Categories = new[] { CategoryPDF, CategoryProductivity },
                Tags = new[] { "pdf", "adobe", "editor" },
                DeclaredPermissions = new[] { "storage", "tabs", "<all_urls>" },
                SuggestedScope = ExtensionScope.Global
            },

            // ═══════════════════════════════════════════════════════════════
            //  🌐 RTL (برای هوش مصنوعی‌ها)
            // ═══════════════════════════════════════════════════════════════

            // ── AI RTL (Persian) ──
            new ExtensionCatalogItem
            {
                Id = "catalog.ai-rtl-persian",
                Name = "AI RTL (فارسی)",
                Description = "راست‌چین و بهینه‌سازی بیش از ۴۰ هوش مصنوعی برای " +
                              "کاربران فارسی‌زبان. پشتیبانی از فونت فارسی و " +
                              "جهت‌دهی خودکار متن.",
                Version = "1.5.0",
                Author = "AI RTL",
                Glyph = "\uE8C1",
                HomepageUrl = "https://chromewebstore.google.com/detail/ai-rtl-فارسی/",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryRTL, CategoryAccessibility },
                Tags = new[] { "rtl", "persian", "farsi", "ai", "chatgpt", "claude", "gemini" },
                DeclaredPermissions = new[] { "storage", "activeTab" },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Global
            },

            // ── RTL for AI Sites ──
            new ExtensionCatalogItem
            {
                Id = "catalog.rtl-for-ai-sites",
                Name = "RTL for AI Sites",
                Description = "Smart right-to-left support for AI chat platforms " +
                              "like ChatGPT, Claude, Gemini, Grok, DeepSeek, " +
                              "Perplexity, Copilot, Poe, and Arena.",
                Version = "1.1.0",
                Author = "Arman Babaei",
                Glyph = "\uE8C1",
                HomepageUrl = "https://addons.mozilla.org/en-CA/firefox/addon/rtl-for-ai-sites/",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryRTL, CategoryAccessibility },
                Tags = new[] { "rtl", "ai", "chatgpt", "claude", "gemini", "persian" },
                DeclaredPermissions = new[] { "storage", "activeTab" },
                IsFeatured = true,
                SuggestedScope = ExtensionScope.Global
            },

            // ── RTL Fix for AI Chats ──
            new ExtensionCatalogItem
            {
                Id = "catalog.rtl-fix-ai-chats",
                Name = "RTL Fix for AI Chats",
                Description = "Fixes right-to-left text (Hebrew, Arabic, Persian) " +
                              "on AI chats while keeping math, code, and numbers " +
                              "left-to-right.",
                Version = "1.0.0",
                Author = "RTL Fix",
                Glyph = "\uE8C1",
                HomepageUrl = "https://chromewebstore.google.com/detail/rtl-fix-for-ai-chats/",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryRTL, CategoryAccessibility },
                Tags = new[] { "rtl", "arabic", "hebrew", "persian", "ai" },
                DeclaredPermissions = new[] { "storage", "activeTab" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── AI Web Chat RTL Support ──
            new ExtensionCatalogItem
            {
                Id = "catalog.ai-web-chat-rtl",
                Name = "AI Web Chat RTL Support",
                Description = "Automatic RTL (Hebrew/Arabic) support for Claude, " +
                              "ChatGPT, and Gemini. Detects text direction in " +
                              "real time.",
                Version = "1.0.0",
                Author = "AI RTL",
                Glyph = "\uE8C1",
                HomepageUrl = "https://chromewebstore.google.com/detail/ai-web-chat-rtl-support/",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryRTL, CategoryAccessibility },
                Tags = new[] { "rtl", "arabic", "hebrew", "ai", "chat" },
                DeclaredPermissions = new[] { "storage", "activeTab" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── RTLify for Chatbots ──
            new ExtensionCatalogItem
            {
                Id = "catalog.rtlify-chatbots",
                Name = "RTLify for Chatbots",
                Description = "Make mainstream AI chatbots right-to-left and apply " +
                              "your preferred custom font. Supports ChatGPT, " +
                              "Claude, Gemini, and more.",
                Version = "1.0.0",
                Author = "NedaMani",
                Glyph = "\uE8C1",
                HomepageUrl = "https://addons.mozilla.org/en-US/firefox/addon/rtlify-for-chatbots/",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryRTL, CategoryAccessibility },
                Tags = new[] { "rtl", "chatbot", "ai", "persian", "font" },
                DeclaredPermissions = new[] { "storage", "activeTab" },
                SuggestedScope = ExtensionScope.Global
            },

            // ── AI RTL Pro ──
            new ExtensionCatalogItem
            {
                Id = "catalog.ai-rtl-pro",
                Name = "AI RTL Pro",
                Description = "Advanced RTL support for AI chat platforms including " +
                              "ChatGPT, Claude, Gemini, DeepSeek, Perplexity, Grok, " +
                              "Copilot, Poe, Qwen, Mistral, and OpenRouter.",
                Version = "2.0.0",
                Author = "Ashkan Kiani",
                Glyph = "\uE8C1",
                HomepageUrl = "https://addons.mozilla.org/en-US/firefox/addon/ai-rtl-pro/",
                Source = ExtensionSourceType.Community,
                Categories = new[] { CategoryRTL, CategoryAccessibility },
                Tags = new[] { "rtl", "ai", "persian", "chatgpt", "claude", "gemini" },
                DeclaredPermissions = new[] { "storage", "activeTab" },
                SuggestedScope = ExtensionScope.Global
            }
        };
    }
}