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
- [ ] Implement MainWindow and Shell navigation framework — `PLANNED`
- [ ] Implement modern dark color palette (charcoal/slate/indigo) — `PLANNED`
- [ ] Implement Accounts page with virtualized account cards — `PLANNED`
- [ ] Implement Settings & Security overview panels — `PLANNED`

### Phase 3: SQLite Database & Account CRUD
- [ ] SQLite connection manager & auto-migration engine — `PLANNED`
- [ ] Repository implementations (`AccountRepository`) with parameterized queries — `PLANNED`
- [ ] Account CRUD unit & integration tests — `PLANNED`

### Phase 4: DPAPI Security & Credential Management
- [ ] Implement `DpapiEncryptionService` with `DataProtectionScope.CurrentUser` — `PLANNED`
- [ ] Sensitive memory disposal helpers — `PLANNED`
- [ ] Automated security tests for encryption roundtrips and tampered payloads — `PLANNED`

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

