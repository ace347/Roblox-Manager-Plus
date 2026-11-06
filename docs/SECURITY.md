# Security Policy & Defensive Threat Model

## 1. Threat Model & Assets

The primary assets requiring protection in this application are:
1. **User Authentication Tokens / Credentials:** Stored on the local disk.
2. **Account Metadata & Activity Logs:** Usernames, launch timestamps, and notes.
3. **Application Integrity:** Prevention of tampering or unauthorized local process interference.

### Threat Actors & Scenarios
- **Non-Elevated Malicious Local Processes:** Untrusted background software running under another user account or sandboxed context trying to steal stored credentials.
  - *Defense:* Windows DPAPI with `CurrentUser` scope ensures only processes running under the exact same Windows user context can decrypt the blob.
- **Accidental Leakage via Logs / Crash Dumps:** Credentials inadvertently dumped in plaintext logs.
  - *Defense:* Serilog masking filters, omission of secrets in exception messages, immediate memory wiping.
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
- **Per-Record Entropy:** Every encrypted secret is paired with an independently generated 32-byte cryptographic salt (`RandomNumberGenerator.Fill(entropy)`), preventing correlation across entries.
- **Zero Fallback:** If DPAPI is unavailable or throws a `CryptographicException`, the application fails safely. There is **zero plaintext fallback**.

### 2.2 In-Memory Lifetime & Memory Wiping
- All sensitive cryptographic keys and session secrets are wrapped in [`SensitiveBuffer`](file:///c:/Users/A%20S%20U%20S/Documents/REEA/src/RobloxAccountManager.Core/Security/SensitiveBuffer.cs).
- On disposal, `CryptographicOperations.ZeroMemory` is called immediately on the underlying byte array.
- **Honest Managed Memory Limitation:** While `ZeroMemory` wipes the active pinned byte array, the .NET CLR and Garbage Collector may have relocated or cached transient representations in virtual memory prior to disposal. DPAPI current-user protection serves as the primary disk defense; `SensitiveBuffer` serves as defense-in-depth against lingering process memory.

### 2.3 Residual Risks & What DPAPI Does NOT Protect Against
- **Same-User Process Inspection:** Any process running under the exact same Windows user account (with identical user SID/privileges) can theoretically invoke `ProtectedData.Unprotect` if it extracts both the encrypted blob and per-record entropy.
- **Kernel / SYSTEM Privilege Escalation:** An attacker with local Administrator or `NT AUTHORITY\SYSTEM` privileges can dump process memory or acquire DPAPI system keys.
- **Compromised Machine / Keyloggers:** DPAPI protects data at rest; it cannot protect against active ring-3 userland keyloggers running under the same user session.

---

## 3. Binary Authenticity & Authenticode Verification

To defend against binary spoofing, DLL hijacking, and modified/malicious executables placed in Roblox directories:

### 3.1 Verification Architecture (`WinTrustAuthenticodeVerifier`)
- **Native Subsystem:** Leverages Win32 `WinVerifyTrust` (`wintrust.dll`) with the `WINTRUST_ACTION_GENERIC_VERIFY_V2` policy provider.
- **State Action Lifecycle:** Follows the full two-stage WinTrust state lifecycle: `WTD_STATEACTION_VERIFY` followed by an explicit `WTD_STATEACTION_CLOSE` call to release provider-allocated state structures (`hWVTStateData`) and prevent native memory leaks.
- **Cryptographic Digest Validation:** Evaluates PE file checksums and Authenticode hashes. If a signed executable has been tampered with or corrupted, WinVerifyTrust returns `TRUST_E_BAD_DIGEST` (`0x80096010`).
- **Trust Chain & Root CA:** Checks that the certificate chain culminates in a trusted Root Certification Authority in the Windows certificate store. Untrusted roots return `CERT_E_UNTRUSTEDROOT` (`0x800B0109`).
- **Revocation Enforcement:** Configured with:
  - `WINTRUST_DATA.fdwRevocationChecks = WTD_REVOKE_WHOLECHAIN` (`0x00000001`): Mandates certificate revocation evaluation across the entire certificate chain (leaf, intermediate, and root).
  - `WINTRUST_DATA.dwProvFlags |= WTD_REVOCATION_CHECK_CHAIN` (`0x00000040`): Configures provider revocation flags across the chain.
  - `WINTRUST_DATA.dwProvFlags |= WTD_CACHE_ONLY_URL_RETRIEVAL` (`0x00001000`): Enforces offline-safe evaluation from local system CRL/OCSP cache without incurring blocking network hangs.
- **Distinction for Offline Revocation:** When revocation information is unavailable or offline, the verifier explicitly returns `AuthenticodeStatus.RevocationOfflineOrUnavailable` (`0x80092013` / `CRYPT_E_REVOCATION_OFFLINE`) rather than claiming fresh online verification or silently ignoring revocation failure.
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

---

## 4. Strict Prohibitions

1. **No Code Injection:** We do not attach debuggers, inject DLLs, or modify memory of `RobloxPlayerBeta.exe`.
2. **No Browser Cookie Harvesting:** We never read session databases from Chrome, Edge, or Firefox.
3. **No Auth Bypass:** We rely exclusively on official authentication pathways and client protocols.

