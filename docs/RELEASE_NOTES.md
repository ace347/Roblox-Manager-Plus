# Release Notes — Roblox Account Manager Plus v1.0.0

**Release Date:** November 2026  
**Runtime Target:** Windows 10 / 11 (x64) — .NET 10  
**Repository:** [https://github.com/ace347/Roblox-Manager-Plus](https://github.com/ace347/Roblox-Manager-Plus)

---

## 1. Overview

Roblox Account Manager Plus is a high-security, privacy-respecting account management and launcher dashboard for Windows. Designed from the ground up under clean-room engineering principles, it provides seamless multi-profile organization without ever exposing credentials, tampering with client processes, or violating third-party platform terms.

---

## 2. Key Features

- **Windows DPAPI Security:** Sensitive session data and account secrets are encrypted using Windows Data Protection API (`DataProtectionScope.CurrentUser`) combined with unique per-record cryptographic entropy.
- **Zero-Plaintext In-Memory Handling:** Cryptographic payloads are managed via `SensitiveBuffer` wrappers and scrubbed immediately using `CryptographicOperations.ZeroMemory`.
- **Authenticode Signature Verification:** Discovered and launched Roblox client executables (`RobloxPlayerBeta.exe`) are validated via native Win32 `WinVerifyTrust` APIs with whole-chain revocation checks (`WTD_REVOKE_WHOLECHAIN`) and exact publisher Common Name matching (`Roblox Corporation`).
- **Safe Process Invocation:** Launches use verified filesystem paths with parameterized argument lists (`roblox://` protocol only). No shell execution, command injection, or memory manipulation.
- **Client Session Tracking:** Clean process lifecycle tracking using read-only OS process IDs without process injection or DLL hooking.
- **Modern Dark-Themed UI:** Responsive WPF interface built with MVVM, dark charcoal/slate palette, and virtualized list containers for smooth rendering of thousands of records.
- **SQLite WAL Database:** Parameterized SQLite database with Write-Ahead Logging (WAL) mode, foreign key enforcement, and automated corruption backup recovery.

---

## 3. Distribution Packages

| Package | File | Size | Description |
| :--- | :--- | :--- | :--- |
| **Portable ZIP** | `RobloxAccountManager-v1.0.0-win-x64.zip` | ~2.2 MB | Portable archive containing all binaries, native SQLite dependencies, and runtime configurations. |
| **Single-File Binary** | `artifacts/single-file/RobloxAccountManager.App.exe` | ~4.3 MB | Consolidated executable with extracted native dependencies for easy deployment. |

---

## 4. System Requirements

- **Operating System:** Windows 10 (Version 1809+) or Windows 11 (64-bit).
- **Runtime:** Microsoft .NET 10.0 Desktop Runtime (x64).
- **Client:** Official Roblox Windows Desktop App (`RobloxPlayerBeta.exe`).
