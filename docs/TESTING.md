# Testing Strategy & Quality Assurance

## 1. Test Pyramid

Automated tests cover domain behavior, metadata CRUD, DPAPI, SQLite, logging redaction, and controlled Windows executable verification/launch behavior. They do not prove complete application security or UI usability.

- **Unit Tests:** Fast, isolated tests for domain models, validation, and encryption utilities.
- **Integration Tests:** SQLite repository persistence, migration validation, and DPAPI roundtrip testing.
- **UI & Presentation Tests:** ViewModel state management and relay command validations using mock services.

---

## 2. Test Projects

- `tests/RobloxAccountManager.Core.Tests`: Account model defaults and `SensitiveBuffer` zeroing/disposal.
- `tests/RobloxAccountManager.Application.Tests`: Account metadata creation, empty username rejection, and duplicate rejection.
- `tests/RobloxAccountManager.Infrastructure.Tests`: SQLite repository setup, DPAPI and `CredentialVault`, log redaction, WinTrust, client discovery, and process launcher checks.
- `tests/RobloxAccountManager.App.Tests`: Account and dashboard ViewModel state, navigation, and add-dialog initialization.

---

## 3. Verified Run

The Phase 7 verification run completed on Windows with **96 passed, 0 failed, 0 skipped** across all 4 test projects:
- `RobloxAccountManager.Core.Tests`: 7 passed
- `RobloxAccountManager.Application.Tests`: 14 passed (including `EndToEndWorkflowTests`)
- `RobloxAccountManager.Infrastructure.Tests`: 60 passed (including `PerformanceAndStressTests`, `DatabaseInitializerRecoveryTests`, and WinTrust verifier)
- `RobloxAccountManager.App.Tests`: 15 passed (including `MainWindowNavigationTests` and `SessionsViewModelTests`)

The Release build compiles with **0 warnings, 0 errors**.

## 4. Performance & Stress Testing (Phase 7)

Phase 7 introduces automated stress and performance benchmarks in `tests/RobloxAccountManager.Infrastructure.Tests/PerformanceAndStressTests.cs`:

- **Bulk SQLite Insertion & Query SLA**: 500 accounts inserted and queried under SQLite WAL mode completes in $< 1$ second ($< 250\text{ ms}$ query/search latency).
- **DPAPI Throughput & Zero-Leak Verification**: 200 high-frequency encrypt/decrypt roundtrips verify cryptographic stability, constant memory consumption, and zero buffer retention.
- **Concurrent Read/Write Stress**: 10 simultaneous asynchronous workers performing 200 total operations against SQLite with zero locking errors or database corruption.
- **WPF UI Virtualization**: `AccountsView` and `SessionsView` employ `VirtualizingStackPanel.VirtualizationMode="Recycling"` and `ScrollUnit="Pixel"`, rendering only items visible in the viewport.

## 5. Automated Coverage and Gaps

| Test Case | Method | Expected Result |
|---|---|---|
| **DPAPI** | Roundtrip, empty data, wrong entropy, corrupted ciphertext, 1 MB payload | Roundtrip succeeds; invalid inputs fail or preserve the documented empty behavior. |
| **Credential vault** | Store/retrieve and clear using a temporary SQLite database | DB stores ciphertext; clear removes the logical secret value. |
| **SQLite** | CRUD, search, default account, WAL, foreign-key connection flag | Parameterized operations succeed on isolated temp databases. |
| **Sensitive buffer** | Retain underlying array across disposal | Array is zeroed and subsequent access throws. |
| **Log redaction** | Structured scalar string containing a token pattern | Matching text is replaced before sink output. Nested and exception values are not comprehensively tested. |
| **WinTrust** | Signed Windows fixture, unsigned file, modified signed file, publisher mismatch, expected publisher, missing file | Statuses match expected Windows trust results. Does not test an actual Roblox-signed binary. |
| **Launcher** | Invalid signature, URL rejection, path validation, controlled launch | Invalid trust result is rejected; controlled fixture is launched without shell arguments. |

Not currently automated: cross-Windows-user DPAPI isolation; CredentialVault repository/encryption failure combinations; database corruption recovery, failed migrations, and concurrent stress; application startup/shutdown and unavailable-database UX; real Roblox publisher/signature checks; certificate revocation freshness; process launch failure under OS policy; WPF accessibility, high-DPI, resizing, and keyboard navigation; startup/rendering/search timing and memory/idle CPU benchmarks.

## 5. Manual Windows Verification

The above gaps require a separate disposable Windows profile and temporary test data before claiming full Phase 6 completion. Do not point tests at a user's normal vault or use real Roblox credentials. No actual Roblox client launch was performed as part of this audit.

