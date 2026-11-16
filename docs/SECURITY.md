# Security Policy & Defensive Threat Model

## 1. Threat Model & Assets

The application stores account metadata locally. `CredentialVault` can encrypt caller-provided byte buffers, but the current UI does not collect credentials and the launcher does not use or switch authenticated sessions. No real Roblox credentials are part of the test suite.

### Threat Actors & Scenarios
- **Non-Elevated Malicious Local Processes:** Untrusted background software running under another user account or sandboxed context trying to steal stored credentials.
  - *Defense:* Windows DPAPI with `CurrentUser` scope ensures only processes running under the exact same Windows user context can decrypt the blob.
- **Accidental Leakage via Logs / Crash Dumps:** Structured string properties matching known token/password patterns are redacted before the file sink writes them. This is best-effort, not a guarantee: nested values, exception text, and unlabelled secrets are not comprehensively scrubbed. Secrets must not be logged.
- **Supply Chain / Package Tampering:** Rogue third-party dependencies introducing backdoors.
  - *Defense:* Central Package Management with pinned versions, zero untrusted binary packages, strictly minimal dependency footprint.

---

## 2. Cryptographic Architecture (Windows DPAPI & CredentialVault)

### 2.1 Encryption Standard
Windows Data Protection API (DPAPI) is used exclusively:
```csharp
byte[] encryptedData = ProtectedData.Protect(
    plaintextBytes, 
    optionalEntropy, 
    DataProtectionScope.CurrentUser
);
```

- **Scope:** `DataProtectionScope.CurrentUser`. The master key is derived directly from the logged-in Windows user's credential hierarchy.
- **Per-Record Entropy:** The vault creates a random 32-byte optional entropy value per stored secret and stores it beside the ciphertext. It is not a secret key and does not add protection against a process running as the same Windows user.
- **Zero Fallback:** If DPAPI is unavailable or throws a `CryptographicException`, the application fails safely. There is **zero plaintext fallback**.

### 2.2 In-Memory Lifetime & Memory Wiping
- Sensitive byte buffers can be wrapped in [`SensitiveBuffer`](../src/RobloxAccountManager.Core/Security/SensitiveBuffer.cs). Its constructor clones the input; disposal zeroes the wrapped array, not the caller's original array or other copies.
- The array is not pinned. The CLR may have made copies or moved data before disposal; zeroing one managed array is defense-in-depth, not guaranteed process-memory erasure.

### 2.3 Residual Risks & What DPAPI Does NOT Protect Against
- **Same-User Process Inspection:** Any process running under the exact same Windows user account (with identical user SID/privileges) can theoretically invoke `ProtectedData.Unprotect` if it extracts both the encrypted blob and per-record entropy.
- **Kernel / SYSTEM Privilege Escalation:** An attacker with local Administrator or `NT AUTHORITY\SYSTEM` privileges can dump process memory or acquire DPAPI system keys.
- **Compromised Machine / Keyloggers:** DPAPI protects data at rest; it cannot protect against active ring-3 userland keyloggers running under the same user session.
- **SQLite deletion:** Clearing or deleting a record removes the logical value, but does not guarantee forensic erasure from database pages, WAL files, snapshots, or storage-device remapping.

---

## 3. Binary Authenticity & Authenticode Verification

To defend against binary spoofing, DLL hijacking, and modified/malicious executables placed in Roblox directories:

### 3.1 Verification Architecture (`WinTrustAuthenticodeVerifier`)
- **Native Subsystem:** Leverages Win32 `WinVerifyTrust` (`wintrust.dll`) with the `WINTRUST_ACTION_GENERIC_VERIFY_V2` policy provider.
- **State Action Lifecycle:** Performs `WTD_STATEACTION_VERIFY` followed by best-effort `WTD_STATEACTION_CLOSE` when provider state exists.
- **Cryptographic Digest Validation:** Evaluates PE file checksums and Authenticode hashes. If a signed executable has been tampered with or corrupted, WinVerifyTrust returns `TRUST_E_BAD_DIGEST` (`0x80096010`).
- **Trust Chain & Root CA:** Checks that the certificate chain culminates in a trusted Root Certification Authority in the Windows certificate store. Untrusted roots return `CERT_E_UNTRUSTEDROOT` (`0x800B0109`).
- **Revocation Policy:** Configured with:
  - `WINTRUST_DATA.fdwRevocationChecks = WTD_REVOKE_WHOLECHAIN` (`0x00000001`): Mandates certificate revocation evaluation across the entire certificate chain (leaf, intermediate, and root).
  - `WINTRUST_DATA.dwProvFlags |= WTD_REVOCATION_CHECK_CHAIN` (`0x00000040`): Configures provider revocation flags across the chain.
  - `WINTRUST_DATA.dwProvFlags |= WTD_CACHE_ONLY_URL_RETRIEVAL` (`0x00001000`): Enforces offline-safe evaluation from local system CRL/OCSP cache without incurring blocking network hangs.
- **Offline Revocation Limitation:** The policy is cache-only. The verifier may reject when revocation data is unavailable, and it cannot observe newly published revocations until the local cache is refreshed.
- **Publisher Identity Match & Organization Validation:**
  - Extracts the exact Subject Common Name (`X509NameType.SimpleName`, e.g., `"Roblox Corporation"`) and verifies exact match against authorized publishers, preventing substring spoofing (e.g. `CN=Roblox Corporation Fake`).
  - **Limitations of Subject CN Alone:** A subject Common Name alone does not guarantee authenticity unless coupled with cryptographic trust chain validation to a trusted root authority. A self-signed certificate can easily forge `CN=Roblox Corporation`. Therefore, `WinVerifyTrust` trust-chain validation is strictly mandatory before publisher identity is evaluated.
  - **Additional Organization Identity (O) Checks:** While the Subject Distinguished Name also contains `O=Roblox Corporation` and `SERIALNUMBER=3780902`, exact CN matching against an Authenticode-verified chain provides the necessary protection without brittle binding to organizational metadata that changes during corporate restructuring or EV/OV issuance transitions.
- **Certificate Rollover Policy:** Roblox Corporation periodically rotates its code-signing certificates upon expiration (e.g., renewed DigiCert EV/OV code signing certificates). The system matches the canonical publisher Subject Common Name (`Roblox Corporation`), which remains stable across certificate renewals while requiring that each new certificate chains to an OS-trusted root.
- **Size Sanity vs. Security:** Executable size checks (>= 65,536 bytes) and path validation are strictly initial sanity checks; they are **never** treated as cryptographic proof of authenticity.

### 3.2 Security Guarantees & Platform Limitations
- **Platform Specificity:** Win32 `WinVerifyTrust` is exclusive to Microsoft Windows.
- **Revocation Offline Window:** Cached revocation checking (`WTD_CACHE_ONLY_URL_RETRIEVAL`) relies on local CRL/OCSP caches. It cannot detect a newly revoked certificate if the revocation list has not yet been fetched to the local machine and network fetching is prevented. When cached revocation data is missing/stale, `WinVerifyTrust` fails safely with `CRYPT_E_REVOCATION_OFFLINE`.
- **Local Administrator Trust:** An attacker with local Administrator privileges can install a rogue root certificate into the local machine's Trusted Root Certification Authorities store. Publisher subject verification provides secondary defense against this scenario.

### 3.3 Current Enforcement
- Discovery filters out binaries that fail the exact `Roblox Corporation` publisher check.
- Launch performs a fresh WinTrust verification immediately before `Process.Start`; an invalid or unavailable signature is rejected. File replacement between verification and process open remains a time-of-check/time-of-use limitation.
- The application source currently contains no HTTP client or telemetry implementation. The avatar endpoint described in `API_INTEGRATION.md` is not wired into the application.
- The configured file logger redacts matching patterns in top-level structured string properties. This does not cover every possible serialization or exception path, so callers must not pass secrets into logs.

### 3.4 Authentication and Session Boundary
- Roblox documents OAuth 2.0/OIDC for registered Open Cloud apps. That flow can verify an identity to the app and authorize selected Open Cloud resources; it is not Roblox Player authentication and is not wired into this app.
- RAM Plus does not store passwords, cookies, OAuth tokens, or Player session tokens. Every saved profile and managed process is displayed with authentication unverified.
- Managed process records are associated only with a process created by RAM Plus and verified by PID, exact start time, and executable path. Other Roblox Player processes are detected only to prevent a duplicate launch; they are never attached to a saved account.
- No documented official multi-instance mechanism was found in the reviewed Roblox docs. This application permits only one active managed or external Player process at a time and does not bypass client protections.

---

## 4. Strict Prohibitions

1. **No Code Injection:** We do not attach debuggers, inject DLLs, or modify memory of `RobloxPlayerBeta.exe`.
2. **No Browser Cookie Harvesting:** We never read session databases from Chrome, Edge, or Firefox.
3. **No Auth Bypass:** We rely exclusively on official authentication pathways and client protocols.

