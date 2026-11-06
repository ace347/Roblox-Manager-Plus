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
- [ ] Detect official Roblox client installations across LocalAppData/ProgramFiles — `PLANNED`
- [ ] Implement clean argument execution avoiding shell interpretation — `PLANNED`
- [ ] Process status tracking and telemetry-free launch coordination — `PLANNED`

### Phase 6: Hardening, Sanitization & Privacy Assurance
- [ ] Serilog sensitive token masking filter — `PLANNED`
- [ ] Verify zero background telemetry or third-party tracking — `PLANNED`
- [ ] Export/Import with user-provided passphrase & key derivation — `PLANNED`

### Phase 7: Optimization, Performance & Full Test Suite
- [ ] UI virtualization verification for large account datasets — `PLANNED`
- [ ] Startup time benchmarks (< 1.5s target) — `PLANNED`
- [ ] End-to-end integration test execution — `PLANNED`

### Phase 8: Packaging & Release Preparation
- [ ] Production build and single-file publish profiling — `PLANNED`
- [ ] Installer setup configuration — `PLANNED`
- [ ] User documentation and release notes — `PLANNED`

