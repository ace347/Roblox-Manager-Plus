# Secure Roblox Account Manager — Private Edition

A clean, modern, secure Windows desktop application engineered for managing multiple Roblox accounts. Built independently with C# and .NET 10, utilizing WPF with modern UI styling, local SQLite storage, and Windows Data Protection API (DPAPI).

---

## Key Highlights

- **Local & Private:** Zero external tracking, telemetry, or mandatory cloud backend. Data stays on your computer.
- **Windows DPAPI Encryption:** Sensitive credentials and session tokens are encrypted at rest using machine/user-bound DPAPI (`CurrentUser` scope).
- **Modern WPF Dashboard:** Dark-themed UI, responsive MVVM architecture, virtualized lists for smooth interaction.
- **Strict Defensive Standards:** No browser cookie scraping, no process code injection, and no unverified workarounds.

---

## Technology Stack

- **Platform:** Windows 10/11 (x64)
- **Language & Runtime:** C# 14 / .NET 10 (WPF)
- **Database:** SQLite (via `Microsoft.Data.Sqlite`)
- **MVVM Framework:** `CommunityToolkit.Mvvm`
- **UI Components:** `Wpf.Ui`
- **Encryption:** Windows DPAPI (`System.Security.Cryptography.ProtectedData`)
- **Logging:** Serilog with sensitive-data masking

---

## Repository Structure

```
├── src/
│   ├── RobloxAccountManager.App/            # WPF Presentation & Views
│   ├── RobloxAccountManager.Core/           # Domain Entities & Interfaces
│   ├── RobloxAccountManager.Application/    # Business Logic & Use Cases
│   ├── RobloxAccountManager.Infrastructure/ # SQLite, DPAPI, Process Launchers
│   └── RobloxAccountManager.Shared/         # Utilities & Constants
├── tests/
│   ├── Core.Tests/
│   ├── Application.Tests/
│   ├── Infrastructure.Tests/
│   └── App.Tests/
├── docs/                                    # Architecture, Threat Model, Database Schema
└── scripts/                                 # Build, Test, and Packaging automation
```

---

## Getting Started

### Prerequisites
1. **.NET 10 SDK** (x64)
2. **Git**
3. **Windows 10 Version 19041+ or Windows 11**

### Quick Setup & Build
Run the automated PowerShell setup and build scripts:

```powershell
./scripts/setup.ps1
./scripts/build.ps1
./scripts/test.ps1
```

For full setup and developer instructions, see [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).

---

## Security & Ethics Policy

See [docs/SECURITY.md](docs/SECURITY.md) for vulnerability reporting and our defensive threat model. This software does not attempt to bypass Roblox authentication protocols or inject unauthorized code into running game processes.
