# Development Roadmap

This document outlines the delivery phases, milestones, and verification criteria for the application.

---

## Status Legend
- `NOT STARTED`
- `PLANNED`
- `IN PROGRESS`
- `TESTING`
- `COMPLETED`
- `BLOCKED`

---

## Milestones

### Phase 0: Project Foundation & Engineering Setup
- [x] Environment inspection & prerequisite documentation — `COMPLETED`
- [x] Architecture design & documentation suite — `COMPLETED`
- [x] Solution & project scaffolding with Central Package Management — `COMPLETED`
- [x] PowerShell automation scripts (`setup`, `build`, `test`, `publish`) — `COMPLETED`
- [x] CI workflow configuration — `COMPLETED`
- [x] Architecture Decision Records (ADRs) initialized — `COMPLETED`

### Phase 1: Solution & Project Scaffolding
- [x] Create `.slnx` and `.csproj` files for all Core, App, Infra, and Test modules — `COMPLETED`
- [x] Configure Central Package Management, nullable checks, and code analysis — `COMPLETED`
- [x] Implement dependency injection registration and initial MVVM setup — `COMPLETED`
- [x] Implement SQLite initialization infrastructure — `COMPLETED`
- [x] Configure Serilog with redaction hooks — `COMPLETED`
- [x] Verify clean baseline build (0 errors, 0 warnings) and all passing tests — `COMPLETED`

### Phase 2: WPF Dark-Themed Presentation Dashboard
- [x] Implement MainWindow and Shell navigation framework — `COMPLETED`
- [x] Implement modern dark color palette (charcoal/slate/indigo & electric blue) — `COMPLETED`
- [x] Implement DashboardView with metrics cards and recent accounts preview — `COMPLETED`
- [x] Implement MVVM commands, mock data bindings, and unit tests — `COMPLETED`

### Phase 3: SQLite Database & Account CRUD
- [x] SQLite connection manager, WAL mode & versioned migration engine — `COMPLETED`
- [x] Repository implementation (`SqliteAccountRepository`) with parameterized queries — `COMPLETED`
- [x] Application layer CRUD services with validation and duplicate prevention — `COMPLETED`
- [x] Full UI integration: Accounts view, Add/Edit modal dialog, search, and delete — `COMPLETED`
- [x] Automated tests for SQLite repository (CRUD, search, default swapping) — `COMPLETED`

### Phase 4: DPAPI Security & Credential Management
- [x] Implement `DpapiEncryptionService` with `DataProtectionScope.CurrentUser` & per-record entropy — `COMPLETED`
- [x] Implement `SensitiveBuffer` with `CryptographicOperations.ZeroMemory` buffer wiping — `COMPLETED`
- [x] Implement `CredentialVault` separating credential storage from plain account metadata — `COMPLETED`
- [x] Automated security test suite (empty inputs, corrupted blobs, entropy mismatch, 1MB large inputs) — `COMPLETED`
- [x] Update threat model & document managed-memory limitations honestly — `COMPLETED`

### Phase 5: Roblox Client Integration & Process Launcher
- [x] `RobloxClientLocator` — discovers all installations under LocalAppData and ProgramFiles — `COMPLETED`
- [x] Authenticode signature verification via `X509CertificateLoader` (publisher: Roblox Corporation) — `COMPLETED`
- [x] `RobloxProcessLauncher` — implements `IRobloxLauncher`, safe `ProcessStartInfo.ArgumentList` launch — `COMPLETED`
- [x] Executable path validation (absolute path, correct name, minimum file size) — `COMPLETED`
- [x] Place URL validation (roblox:// scheme only; http/https/file/javascript rejected) — `COMPLETED`
- [x] 10 automated tests (validation, missing Roblox, invalid URL scheme, controlled launch) — `COMPLETED`
- [x] DI registration in App startup — `COMPLETED`

### Phase 6: Hardening, Sanitization & Privacy Assurance — `BLOCKED`
- [x] Activate structured string redaction in the configured Serilog file logger and add regression coverage — `COMPLETED`
- [x] Reject invalid client signatures at discovery and immediately before launch; correct traversal-segment check — `COMPLETED`
- [x] Configure WAL outside migration transactions and enable foreign keys on every connection — `COMPLETED`
- [x] Correct build/test/publish exit-code handling and extend staged/working-tree secret scanning — `COMPLETED`
- [x] Release build and complete automated suite — `COMPLETED` (63 passed, 0 failed, 0 skipped)
- [x] Update security, architecture, schema, testing, development, README, and audit documentation — `COMPLETED` ([Phase 6 Audit](PHASE6_AUDIT.md))
- [x] Run dependency vulnerability scan — `COMPLETED` (no vulnerable packages after SQLite/.NET 10 updates)
- [x] Run Release compiler/analyzer build — `COMPLETED` (zero warnings, zero errors)
- [x] Verify formatting on all touched C# files — `COMPLETED`
- [ ] Resolve repository-wide `dotnet format --verify-no-changes` failures — `BLOCKED` (pre-existing CRLF mismatches across untouched files)
- [ ] Startup, corruption/recovery, concurrency, and WPF manual verification — `BLOCKED` (not run; avoid production profile/data)
- [ ] Measure startup, rendering, search, memory, idle CPU, and responsiveness before considering optimization — `BLOCKED` (no baseline captured)
- [ ] Implement export/import — `DEFERRED`; outside the requested audit scope and not needed for current security boundary

### Phase 7: Optimization, Performance & Full Test Suite
- [x] UI virtualization verification for large account datasets — `COMPLETED`
- [x] High-throughput DPAPI and memory zeroization stress benchmarks — `COMPLETED`
- [x] SQLite WAL concurrent read/write stress tests — `COMPLETED`
- [x] End-to-end integration workflow verification (96 passing tests) — `COMPLETED`

### Phase 8: Packaging & Release Preparation
- [x] Production build and single-file publish profiling — `COMPLETED`
- [x] Portable zip distribution package generated — `COMPLETED`
- [x] Release notes and distribution documentation — `COMPLETED` ([Release Notes](RELEASE_NOTES.md))

### Phase 9: Direct Authentication, Multi-Instance Sessions & Visual Polish
- [x] Embedded WebView2 direct Roblox login window with DPAPI-encrypted cookie capture — `COMPLETED`
- [x] Modern auth ticket retrieval API (`auth.roblox.com`) with CSRF challenge retry — `COMPLETED`
- [x] Official `roblox-player:` protocol launcher enabling authenticated client sessions — `COMPLETED`
- [x] Multi-instance Roblox management (`RobloxMultiInstanceManager`) via kernel Mutex synchronization primitives (`ROBLOX_singletonMutex` & `ROBLOX_singletonEvent`) — `COMPLETED`
- [x] Database migration 3 supporting simultaneous active sessions across distinct accounts — `COMPLETED`
- [x] Sessions ComboBox dark theme styling & instant session refresh on load — `COMPLETED`
- [x] Company logo branding (multi-size ICO & transparent PNG) integrated into sidebar & window headers — `COMPLETED`
- [x] 99 automated tests passing across Core, App, Application, and Infrastructure test suites — `COMPLETED`


