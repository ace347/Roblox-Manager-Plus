# Official Roblox API & Client Integration

## 1. Scope & Objective

This document defines how Secure Roblox Account Manager interacts with Roblox endpoints and desktop client processes without compromising user security or violating terms.

---

## 2. Public API Usage

The application uses official, unauthenticated public endpoints solely for displaying user identity elements (e.g., avatar thumbnails):

### Avatar Thumbnail Retrieval
- **Endpoint:** `https://thumbnails.roblox.com/v1/users/avatar-headshot`
- **Method:** `GET`
- **Parameters:**
  - `userIds`: Comma-separated list of target user IDs
  - `size`: `150x150`
  - `format`: `Png`
  - `isCircular`: `true`
- **Cache Strategy:** Thumbnails are cached in `%LOCALAPPDATA%\SecureRobloxAccountManager\cache\avatars\` with an expiration of 7 days to avoid repeated network traffic.

---

## 3. Desktop Client Launching

The application launches the official desktop client installed on the local system:

- **Detection Locations (searched in order):**
  - User install: `%LOCALAPPDATA%\Roblox\Versions\version-*\RobloxPlayerBeta.exe`
  - Machine install (x86): `%PROGRAMFILES(X86)%\Roblox\Versions\version-*\RobloxPlayerBeta.exe`
  - Machine install (x64): `%PROGRAMFILES%\Roblox\Versions\version-*\RobloxPlayerBeta.exe`
- **Preferred Installation:** Newest `LastWriteTime` among valid candidates.
- **Minimum File Size:** 65 536 bytes — files below this threshold are rejected as suspicious stubs.
- **Authenticode Verification:** Publisher subject checked for "Roblox Corporation" via `X509Certificate.CreateFromSignedFile` (the only .NET API that supports Authenticode PE files; `SYSLIB0057` suppressed with targeted `#pragma`).
  - Verification failure does **not** block launch — it is logged as a warning.
  - This protects against accidentally launching impostor executables placed in the same directory.
- **Execution Mechanism:**
  - `System.Diagnostics.Process` with `UseShellExecute = false`.
  - Arguments passed via `ProcessStartInfo.ArgumentList` (no shell string concatenation).
  - Never invoked via `cmd.exe /c` or `powershell.exe`.
  - No DLL injection, memory patching, or API hooking.
- **Permitted Arguments:**
  - `roblox://` URI scheme only (e.g., `roblox://experiences/start?placeId=12345`).
  - All other URI schemes (http, https, file, javascript, etc.) are rejected before launch.
- **No Authentication Injection:**
  - No session tokens, cookies, or credentials are passed as arguments.
  - If Roblox requires authentication, it handles its own login flow internally.
  - This application does **not** implement or bypass Roblox's authentication flow.

### Known Limitation
Roblox does not publish an official documented API for third-party applications to pass session tokens at launch time. This application intentionally leaves that capability unimplemented to remain within documented, sanctioned boundaries. Users must be signed in through the official Roblox client.

