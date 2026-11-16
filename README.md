# Secure Roblox Account Manager — Private Edition

A Windows desktop application for managing local Roblox account metadata. Built independently with C# and .NET 10 using WPF, SQLite, and Windows DPAPI.

---

## Key Highlights

- **Local & Private:** Zero external tracking, telemetry, or mandatory cloud backend. Data stays on your computer.
- **Optional secret vault API:** `CredentialVault` protects secret byte buffers with DPAPI (`CurrentUser` scope). The current UI does not collect credentials or switch authenticated sessions.
- **WPF dashboard:** Dark-themed MVVM interface with account metadata CRUD and search. The account list is not currently virtualized.
- **Strict Defensive Standards:** No browser cookie scraping, no process code injection, and no unverified workarounds.
- **No current network integration:** Avatar endpoints are documented as a future possibility, but no HTTP client or telemetry call is implemented in the application source.

---

## Technology Stack

- **Platform:** Windows 10/11 (x64)
- **Language & Runtime:** C# 14 / .NET 10 (WPF)
- **Database:** SQLite (via `Microsoft.Data.Sqlite`)
- **MVVM Framework:** `CommunityToolkit.Mvvm`
- **UI Components:** `Wpf.Ui`
- **Encryption:** Windows DPAPI (`System.Security.Cryptography.ProtectedData`)
- **Logging:** Serilog file sink with best-effort redaction of matching patterns in top-level structured string properties

---

## Repository Structure

```
├── src/
│   ├── RobloxAccountManager.App/            # WPF Presentation & Views
│   ├── RobloxAccountManager.Core/           # Domain Entities & Interfaces
│   ├── RobloxAccountManager.Application/    # Business Logic & Use Cases
│   ├── RobloxAccountManager.Infrastructure/ # SQLite, DPAPI, Process Launchers
├── tests/
│   ├── RobloxAccountManager.Core.Tests/
│   ├── RobloxAccountManager.Application.Tests/
│   ├── RobloxAccountManager.Infrastructure.Tests/
│   └── RobloxAccountManager.App.Tests/
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

Current capabilities, audit findings, and verification limits are recorded in [docs/PHASE6_AUDIT.md](docs/PHASE6_AUDIT.md).

