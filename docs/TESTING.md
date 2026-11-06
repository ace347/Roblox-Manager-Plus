# Testing Strategy & Quality Assurance

## 1. Test Pyramid

Our testing framework ensures defensive security, data integrity, and UI stability:

- **Unit Tests:** Fast, isolated tests for domain models, validation, and encryption utilities.
- **Integration Tests:** SQLite repository persistence, migration validation, and DPAPI roundtrip testing.
- **UI & Presentation Tests:** ViewModel state management and relay command validations using mock services.

---

## 2. Test Projects

- `tests/Core.Tests`: Validation of models, validation logic, and interfaces.
- `tests/Application.Tests`: Use case coordinators, account switching logic, and event dispatchers.
- `tests/Infrastructure.Tests`: SQLite database queries, DPAPI protection tests, and process launcher unit tests.
- `tests/App.Tests`: ViewModel interaction tests.

---

## 3. Cryptography & Security Testing Matrix

| Test Case | Method | Expected Result |
|---|---|---|
| **DPAPI Roundtrip** | Encrypt plain text -> Decrypt output | Plaintext matches original exactly. |
| **Entropy Verification** | Decrypt with invalid entropy | CryptographicException thrown; no secret revealed. |
| **Tampered Ciphertext** | Flip random bits in encrypted blob | CryptographicException thrown; fails safely. |
| **SQL Injection Check** | Inject SQL injection strings into usernames | Stored safely via parameters; no SQL execution error. |
| **Authenticode: Valid Binary** | Test genuine Windows signed executable | `WinVerifyTrust` succeeds (`S_OK`), status `Valid`. |
| **Authenticode: Unsigned Binary** | Test non-signed file stub | Rejects with `NotSigned` (`0x800B0100`). |
| **Authenticode: Tampered Binary** | Corrupt bytes of signed binary | Rejects with `CorruptedOrModified` (`0x80096010`). |
| **Authenticode: Publisher Mismatch** | Valid Microsoft binary matched against "Roblox" | Signature valid, rejects with `PublisherMismatch`. |
| **Authenticode: Missing File** | Test non-existent path | Fails safely with `FileNotFound`. |

