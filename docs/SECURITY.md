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

## 2. Cryptographic Design (DPAPI)

Windows Data Protection API (DPAPI) uses cryptographic keys derived from the user's Windows login credentials:

```csharp
byte[] encryptedData = ProtectedData.Protect(
    userDataBytes, 
    optionalEntropy, 
    DataProtectionScope.CurrentUser
);
```

- **Scope:** `DataProtectionScope.CurrentUser`.
- **Optional Entropy:** An application-specific salt is mixed into the encryption context to ensure blobs from this application cannot be trivially decrypted by generic DPAPI inspection tools without knowing the contextual entropy.

---

## 3. Strict Prohibitions

1. **No Code Injection:** We do not attach debuggers, inject DLLs, or modify memory of `RobloxPlayerBeta.exe`.
2. **No Browser Cookie Harvesting:** We never read session databases from Chrome, Edge, or Firefox.
3. **No Auth Bypass:** We rely exclusively on official authentication pathways and client protocols.
