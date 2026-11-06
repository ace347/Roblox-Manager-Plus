# AGENTS.md — Mandatory AI Agent Instructions

All AI coding assistants and autonomous agents operating on this repository **must** read and adhere to these guidelines prior to creating or modifying code.

---

## 1. Core Principles & Prohibitions

1. **No Proprietary Code Piracy:** Do not copy code, binaries, or reverse-engineer assets from third-party account managers (including `Roblox.Account.Manager.3.7.2`). Write 100% original, clean implementations.
2. **Never Plaintext Secrets:** Passwords, session cookies, and security tokens must never be written to files, SQLite columns, or logs unencrypted. Always pass through DPAPI (`CurrentUser` scope).
3. **No Process Code Injection:** Never inject DLLs, execute remote threads, or modify memory spaces of the official Roblox client process (`RobloxPlayerBeta.exe`).
4. **No Browser Cookie Stealing:** Do not read or extract session cookies directly from third-party browsers (Chrome, Edge, Firefox, Brave).
5. **No Hallucinated Functionality:** Never claim a security feature is implemented or verified without automated tests demonstrating it.
6. **No Telemetry / Phoning Home:** Keep network calls restricted solely to official, user-requested endpoints (e.g. avatar thumbnail queries). No analytics.

---

## 2. Architecture & Layering Rules

```
RobloxAccountManager.App (UI / WPF)
       │
       ▼
RobloxAccountManager.Application (Services / Use Cases)
       │
       ▼
RobloxAccountManager.Core (Entities / Interfaces / Domain)
       ▲
       │
RobloxAccountManager.Infrastructure (DB / DPAPI / Launcher)
```

- **Domain Isolation:** `RobloxAccountManager.Core` must have **zero** dependencies on external packages, UI frameworks, or SQLite libraries.
- **Presentation Isolation:** `RobloxAccountManager.App` should only contain Views, ViewModels, and UI Converters. It must not execute database queries or call raw crypto APIs directly.
- **Dependency Inversion:** All database, encryption, and process management services in `Infrastructure` must implement interfaces defined in `Core`.

---

## 3. Coding & Security Conventions

- **Nullability:** Treat nullable warnings as errors in security-critical paths (`#nullable enable`).
- **Parameterized Queries:** Use `Microsoft.Data.Sqlite` with parameterized commands. Never concatenate raw SQL strings.
- **Sensitive Memory:** Clear byte arrays containing sensitive keys/passwords using `Array.Clear` as soon as processing completes.
- **Exceptions:** Catch specific exceptions. Never swallow exceptions with empty `catch` blocks.
- **Logging:** Use structured logging with Serilog. Scrub any token/password patterns from output before sinks write to disk.

---

## 4. Verification Workflow

Before reporting any milestone or phase complete:
1. Run `./scripts/build.ps1` to ensure zero compilation errors.
2. Run `./scripts/test.ps1` to confirm all unit and integration tests pass.
3. Update [docs/ROADMAP.md](docs/ROADMAP.md) reflecting exact task status.
