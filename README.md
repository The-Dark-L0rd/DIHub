<div align="center">

# DI Hub

### Unified AI Workspace for Windows

**One app. All your AIs. Fully isolated.**

[![Version](https://img.shields.io/badge/version-3.12.0-blueviolet?style=flat-square)](https://github.com/The-Dark-L0rd)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![WinUI](https://img.shields.io/badge/WinUI-3-blue?style=flat-square)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)

[Features](#-features) • [Screenshots](#-screenshots) • [Installation](#-installation) • [Build](#-build-from-source) • [Security](#-security) • [Contact](#-contact)

</div>

---

## 📖 Overview

**DI Hub** is a professional, native Windows desktop application that unifies all your AI services into one beautiful workspace. Built with **C# / .NET 10 / WinUI 3 / WebView2**, it allows you to:

- Manage **multiple AI services** (ChatGPT, Claude, Gemini, Perplexity, and more)
- Create **multiple isolated accounts** for the same AI service
- Open **up to 4 AIs side-by-side** in the Multi-AI workspace
- Send **prompts to multiple AIs simultaneously** with target selection
- Keep **every account completely isolated** with its own WebView2 profile

No more switching between 10 browser tabs. No more login conflicts. No more lost sessions.

---

## ✨ Features

### 🤖 AI Service Management

- **12 pre-installed AI services**: ChatGPT, Claude, Gemini, Perplexity, Grok, DeepSeek, Copilot, Poe, Mistral, OpenRouter, Google AI Studio, Groq
- **Add custom AI services** with any HTTPS URL
- **Delete services** with automatic cleanup of profiles and tabs
- **Restore defaults** anytime from Command Palette

### 👤 Multi-Account Support

- **Multiple accounts** per AI service (Personal, Work, University, etc.)
- **Fully isolated WebView2 profiles** — each account has its own cookies, cache, and localStorage
- **Login to ChatGPT with 3 different accounts simultaneously** — they never interfere
- **Rename, delete, favorite, or set default** accounts independently

### 🪟 Multi-AI Workspace

- **2, 3, or 4 AI panels** in one window
- **Independent WebView2** in each panel
- **Prompt Composer** to broadcast one prompt to multiple AIs
- **Target Selection** with per-message control
- **Presets** for reusable target sets (Coding Team, Research, ...)
- **Retry Failed** with single click
- **Prompt History** (last 50)
- **Maximize panel** with double-click or `Ctrl+M`
- **Move panels** left/right

### 🎨 Modern UI/UX

- **Dark / Light / System** themes
- **5 accent colors** (Purple, Blue, Cyan, Green, Orange)
- **Mica backdrop** on Windows 11 with graceful fallback
- **Custom Title Bar** with native window controls
- **Smooth animations** (200ms cubic easing, disableable)
- **Command Palette** (`Ctrl+K`) with fuzzy search
- **Toast notifications** with auto-dismiss
- **Empty states** for no-services/no-history scenarios

### ⚡ Performance

- **Lazy WebView2 initialization** — only active tabs consume resources
- **No unnecessary reload** when switching tabs
- **Controlled memory** with proper disposal
- **Fast startup** (under 2 seconds on SSD)

### 🔒 Security

- **No credential extraction** — passwords, tokens, cookies never accessed
- **No authentication bypass** — CAPTCHA and anti-bot protections respected
- **URL validation** — rejects `javascript:`, `data:`, `file:` schemes
- **Log redaction** — URLs and tokens masked in logs
- **Certificate validation** always enabled
- **Config file ACL** restricted to current user

### 💾 Persistence

- **Session restore** — reopen your last session with all tabs
- **Window state** — remembers size and position
- **Multi-monitor validation** — never opens off-screen
- **Workspaces** — save and restore tab sets
- **Settings** persist in `%APPDATA%\DIHub\settings.json`
- **Configuration** persists in `%APPDATA%\DIHub\config.json`

### ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl + K` | Command Palette |
| `Ctrl + L` | Focus Address Bar |
| `Ctrl + T` | New Tab |
| `Ctrl + W` | Close Tab |
| `Ctrl + Shift + T` | Restore Closed Tab |
| `Ctrl + R` | Reload / Reset Layout (Multi-AI) |
| `Ctrl + Tab` | Next Tab / Panel |
| `Ctrl + Shift + Tab` | Previous Tab / Panel |
| `Ctrl + Shift + D` | Developer Tools |
| `Ctrl + Shift + M` | Multi-AI Workspace |
| `Ctrl + Shift + B` | Toggle Sidebar |
| `Ctrl + 1 ... 4` | Activate Panel (Multi-AI) |
| `Ctrl + M` | Maximize Panel (Multi-AI) |
| `Ctrl + ,` | Settings |
| `Esc` | Close Multi-AI / Cancel |

---

## 📸 Screenshots

_Add screenshots here._

---

## 📦 Installation

### Prerequisites

- **Windows 10** version 1809 (build 17763) or later
- **Windows 11** (recommended)
- **WebView2 Runtime** (pre-installed on Windows 11; [download](https://developer.microsoft.com/microsoft-edge/webview2/) for Windows 10)
- **Windows App Runtime 2.5.1** ([download](https://go.microsoft.com/fwlink/?linkid=2222757))

### Install (MSIX)

1. Download `DIHub.APP_3.12.0.0_x64.msix` from the [Releases](https://github.com/The-Dark-L0rd/DIHub/releases) page.
2. Install the certificate:

```powershell
Import-Certificate -FilePath "DIHub.APP_3.12.0.0_x64.cer" -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
```

3. Install the package:

```powershell
Add-AppxPackage -Path "DIHub.APP_3.12.0.0_x64.msix"
```

4. Launch **DI Hub** from the Start Menu.

---

## 🛠 Build from Source

### Requirements

- **Visual Studio 2026** with:
  - Windows application development workload
  - .NET 10 SDK
  - Windows App SDK C# Templates
- **.NET 10 SDK**

### Build

```powershell
git clone https://github.com/The-Dark-L0rd/DIHub.git
cd DIHub
dotnet build DIHub.slnx -c Debug -p:Platform=x64
```

### Run

In Visual Studio, press `F5`.

Or from CLI:

```powershell
dotnet run --project DIHub.APP/DIHub.APP.csproj -c Debug -p:Platform=x64
```

### Test

```powershell
dotnet test DIHub.slnx
```

Expected:

```text
Passed! - Failed: 0, Passed: 112, Skipped: 0, Total: 112
```

---

## 🏗 Architecture

```text
DIHub/
├── src/
│   ├── DIHub.APP/              ← WinUI 3 UI layer
│   │   ├── Controls/           ← Custom controls (WebView2Host, CommandPalette, ToastHost)
│   │   ├── Views/              ← Views (MainView, MultiAIView, SettingsView)
│   │   ├── ViewModels/         ← MVVM ViewModels
│   │   ├── Converters/         ← XAML value converters
│   │   ├── Services/           ← PromptDispatcher, strategies
│   │   └── Themes/             ← Colors, Typography, Spacing
│   │
│   ├── DIHub.Core/             ← Domain logic (no UI dependencies)
│   │   ├── Models/             ← AIService, AIAccount, TabItem, Workspace, ...
│   │   ├── Interfaces/         ← Service contracts
│   │   ├── Services/           ← AIServiceManager, AIAccountManager, ...
│   │   └── Security/           ← UrlPolicy, LogRedactor
│   │
│   ├── DIHub.Infrastructure/   ← External concerns
│   │   ├── Storage/            ← JSON configuration, migrations
│   │   └── Web/                ← BrowserService
│   │
│   └── DIHub.Tests/            ← Unit tests (112 tests)
│
├── Directory.Build.props       ← Global metadata
├── Directory.Packages.props    ← Central Package Management
├── DIHub.slnx                  ← Solution (new .slnx format)
└── README.md
```

### Design Principles

- **Clean Architecture** — UI depends on Core, never the reverse
- **MVVM** with CommunityToolkit.Mvvm
- **Dependency Injection** via Microsoft.Extensions.DependencyInjection
- **Repository Pattern** for configuration storage
- **Strategy Pattern** for provider-specific prompt dispatch
- **Observer Pattern** for reactive updates across the app

---

## 🔒 Security

DI Hub takes security seriously.

### What we NEVER do

- ❌ Extract passwords, tokens, cookies, or session data
- ❌ Log authentication headers or sensitive URLs
- ❌ Bypass CAPTCHA, anti-bot, or authentication
- ❌ Disable SSL/TLS validation
- ❌ Circumvent provider security policies
- ❌ Store secrets in plain text

### What we DO

- ✅ Validate all URLs (reject `javascript:`, `data:`, `file:`)
- ✅ Redact sensitive info from logs
- ✅ Restrict config file ACL to current user
- ✅ Use isolated WebView2 profiles per account
- ✅ Never send telemetry without consent

### Privacy

- **No telemetry** — zero data sent to external servers
- **No analytics** — all operations stay local
- **No cloud sync** — everything stored in `%APPDATA%\DIHub\`
- **No ads** — ever

---

## ⚠️ Known Limitations

- Some AI services may restrict WebView2 login → use **Open in External Browser**
- Automatic prompt dispatch works best with ChatGPT, Claude, Gemini
- For custom services, **Manual** mode may be required
- WebView2 profiles are per-account; same account in two panels is prevented to avoid conflicts
- Response aggregation (comparing AI answers) is planned for a future version

---

## 🗺 Roadmap

- ☑ **v1.0 — Foundation** (MainWindow, tabs, WebView2)
- ☑ **v2.0 — Multi-Account Support**
- ☑ **v3.0 — Multi-AI Workspace**
- ☑ **v3.12 — Settings, Shortcuts, About**
- ☐ **v4.0 — Response Aggregation** (compare AI answers)
- ☐ **v4.1 — Prompt Templates & Library**
- ☐ **v4.5 — Plugin System**

---

## 🤝 Contributing

Contributions are welcome!

1. Fork the repository
2. Create a feature branch:

```bash
git checkout -b feature/amazing
```

3. Commit your changes:

```bash
git commit -m "Add amazing feature"
```

4. Push to the branch:

```bash
git push origin feature/amazing
```

5. Open a Pull Request

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 📞 Contact

**DARK L0RD — Developer & Designer**

- 🐙 GitHub: [@The-Dark-L0rd](https://github.com/The-Dark-L0rd)
- 💬 Telegram: [@DARK_L0RD](https://t.me/DARK_L0RD)

<div align="center">

Built with ❤️ by **DARK L0RD**

© 2026 DARK L0RD. All rights reserved.

</div>
