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

The Phase 6 and UI Release run completed on Windows with **63 passed, 0 failed, 0 skipped** (Core 2, Application 3, Infrastructure 48, App 10). The Release build also succeeded with zero warnings and errors. The prior 53-test count was a user-provided baseline; it was not independently rerun before edits. Ten automated tests were added across the Phase 6 and UI hardening work.

WinTrust integration tests use signed Windows system binaries and require a fixture; they now fail rather than silently pass if no fixture is available. The process-launch test uses a temporary copy of a Windows system executable and does not use a Roblox executable or credentials.

## 4. Automated Coverage and Gaps

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

