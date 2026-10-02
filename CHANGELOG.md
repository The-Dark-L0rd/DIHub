# Changelog

All notable changes to DI Hub will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.10.0] — 2026-10-02

### 🎉 Major Feature: Extensions Platform

DI Hub now supports a full browser extension platform powered by WebView2.

#### Added
- **Extension Manager** — discover, install, enable, disable, and remove extensions
- **Discover tab** — browse a curated catalog of 29 extensions across 8 categories
- **One-click install** — for extensions with a direct download URL (uBlock Origin Lite, Dark Reader, Immersive Translate)
- **Install from URL** — paste a direct ZIP download URL with optional SHA-256 verification
- **Install from ZIP / folder** — local installation with path-traversal protection
- **Scope Management** — assign extensions to Global / Service / Account with multi-select
- **Permission viewer** — see exactly what each extension requests before installing
- **Diagnostics tab** — WebView2 runtime, installed extensions, and assignment status
- **Settings → Extensions** — master switch, local/remote install toggles, safe mode
- **Backup & Restore** — export/import services, extensions, and settings (config only, no cookies)
- **Full Reset** — reset individual sections or everything at once
- **Command Palette** — new commands: Open Extensions, Diagnostics, Disable All Extensions

#### Catalog (29 extensions)
- ⭐ **Essential**: uBlock Origin Lite, Dark Reader, Bitwarden, Google Translate, Immersive Translate, OneTab
- 🤖 **AI**: Sider, HARPA AI, Merlin AI, Monica, MaxAI, AIPRM, ChatGPT
- ✍️ **Writing**: Grammarly, LanguageTool
- 🔬 **Research**: Immersive Translate, Google Translate
- 👨‍💻 **Developer**: React Developer Tools, Wappalyzer, JSON Viewer Pro
- 📚 **Productivity**: Todoist, Notion Web Clipper
- 📄 **PDF**: Adobe Acrobat
- 🌐 **RTL**: AI RTL (فارسی), RTL for AI Sites, RTL Fix for AI Chats, AI Web Chat RTL Support, RTLify for Chatbots, AI RTL Pro

### Added — Other
- **Qwen** added to the default AI services list (13 total now)
- **Multi-select Scope dialog** — apply an extension to multiple services and accounts at once
- **Accent-aware accent color** in the sidebar and accent-sensitive icons
- **Installed indicator** on catalog cards — replaces `[+]` after successful install
- **Minimum window size** enforced via Win32 (`WM_GETMINMAXINFO`) — prevents title bar overlap
- **Adaptive title bar** — icons disappear progressively as the window narrows

### Fixed
- Title bar no longer overlaps caption buttons when the window is narrow
- Extensions dialog resizes with the window (no more overlap)
- Configuration migration v7 → v8 adds Qwen to existing installations
- `EnableAsync(bool)` API compatibility with WebView2 SDK 1.0.3719.77

### Changed
- Config version bumped from 7 → 8 (Qwen migration)
- Extension files stored under `%LOCALAPPDATA%\DIHub\Extensions\`
- Extension config stored separately in `%APPDATA%\DIHub\extensions.json`

### Security
- Path-traversal protection in `SafeZipExtractor`
- SHA-256 verification for URL-based installs
- URL policy: only `https://` allowed for remote extension downloads
- Extension packages are never auto-executed — manifest validation always runs first

---

## [3.12.3] — 2026-09-15
- Inno Setup Installer
- Certificate and single-instance fixes
- XAML compilation fixes

## [3.12.2] — 2026-09-10
- `AIServiceManager` version fixes
- Icon structure improvements

## [3.12.1] — 2026-09-08
- Icon shortcut fix attempt

## [3.12.0] — 2026-09-01 (Initial Release)
- Foundation (Solution + 4 projects)
- DI + Host + Single Instance
- Custom Title Bar + Mica
- WebView2Host with Profile Isolation
- Multi-Account Support
- Tab System
- Multi-AI Workspace (2/3/4 panels)
- Prompt Composer + Broadcast
- Command Palette (Ctrl+K)
- Settings (7 sections)
- Session Restore
- Workspaces
- 112 Unit Tests → now 153

## Before 3.12.0
- Initial architecture design
- Models implementation
- Services implementation
- DI setup
- Theme system
- Migrations v1 → v7