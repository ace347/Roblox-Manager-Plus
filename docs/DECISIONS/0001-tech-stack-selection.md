# ADR-0001: Selection of .NET 10, WPF, and SQLite Stack

## Status
Accepted

## Context
The project requires a secure, high-performance, private Windows desktop application for managing accounts locally with no mandatory cloud backend. The target platform is Windows 10/11 x64.

## Decision
1. **Framework & Language:** C# 14 on .NET 10 (WPF). WPF provides native Windows window management, hardware acceleration, and integration with the Windows Desktop App SDK without web-engine overhead (e.g. Electron/Chromium).
2. **Encryption:** Windows DPAPI (`DataProtectionScope.CurrentUser`). Provides OS-backed cryptographic protection tied directly to the user's Windows profile without storing static symmetric keys in application code.
3. **Database:** SQLite (`Microsoft.Data.Sqlite`) in WAL mode. Zero configuration, embedded, reliable, fully local, and supports parameterized queries.
4. **MVVM Framework:** `CommunityToolkit.Mvvm`. Modern source generators for observable properties and commands without heavy runtime reflection.

## Consequences
- The application is native to Windows and cannot run cross-platform without porting UI to Avalonia/MAUI, which matches the Windows-only scope.
- Central Package Management will ensure aligned versions across all projects.
