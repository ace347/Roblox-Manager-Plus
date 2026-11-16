# Official Roblox API & Client Integration

## 1. Scope & Objective

This document defines how Secure Roblox Account Manager interacts with Roblox endpoints and desktop client processes without compromising user security or violating terms.

---

## 2. Public API Usage

No HTTP/API integration is currently implemented in application source. The endpoint below is a potential future integration only; there are no avatar requests, caches, or background network calls at present.

### Proposed Avatar Thumbnail Retrieval
- **Endpoint:** `https://thumbnails.roblox.com/v1/users/avatar-headshot`
- **Method:** `GET`
- **Parameters:**
  - `userIds`: Comma-separated list of target user IDs
  - `size`: `150x150`
  - `format`: `Png`
  - `isCircular`: `true`
- **Proposed Cache Strategy:** If implemented later, thumbnails could be cached locally with an expiration policy.

---

## 3. Official OAuth 2.0 and Player Authentication

Roblox documents Authorization Code OAuth 2.0 with PKCE and OpenID Connect for authorized Open Cloud applications. The `openid` and `profile` scopes can verify a Roblox identity to the registered app and retrieve profile claims. Open Cloud access tokens authorize the granted Open Cloud resources; an ID token is an identity assertion to the app.

This flow does **not** authenticate `RobloxPlayerBeta.exe`, select the account signed into the local Player, or expose the Player's current sign-in state. The desktop client does not integrate with OAuth in this application. RAM Plus currently has no registered OAuth app/client ID, redirect registration, or reviewed scope, so it does not initiate OAuth or store OAuth tokens.

Official references:
- [OAuth 2.0 overview](https://create.roblox.com/docs/cloud/auth/oauth2-overview)
- [OAuth 2.0 authentication reference](https://create.roblox.com/docs/cloud/auth/oauth2-reference)
- [OAuth 2.0 app registration](https://create.roblox.com/docs/cloud/auth/oauth2-registration)
- [Creator Third-Party App Policy](https://en.help.roblox.com/hc/en-us/articles/37924211313044-Creator-Third-Party-App-Policy)

## 4. Desktop Client Launching

The application launches the official desktop client installed on the local system:

- **Detection Locations (searched in order):**
  - User install: `%LOCALAPPDATA%\Roblox\Versions\version-*\RobloxPlayerBeta.exe`
  - Machine install (x86): `%PROGRAMFILES(X86)%\Roblox\Versions\version-*\RobloxPlayerBeta.exe`
  - Machine install (x64): `%PROGRAMFILES%\Roblox\Versions\version-*\RobloxPlayerBeta.exe`
- **Preferred Installation:** Newest `LastWriteTime` among valid candidates.
- **Minimum File Size:** 65 536 bytes — files below this threshold are rejected as suspicious stubs.
- **Authenticode Verification:** Windows `WinVerifyTrust` checks the signature, trust chain, revocation status according to the local cache policy, and exact publisher identity. Discovery excludes failures; launch repeats verification immediately before process start and fails closed.
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
No official API for passing Player session credentials or querying the Player's authenticated account was found in the reviewed documentation. Users must sign in manually in the official client; the manager labels authentication **unverified**.

### Multiple Player Instances
The reviewed official Roblox OAuth, Open Cloud, and third-party app documentation does not document a supported mechanism for launching multiple independent authenticated Roblox Player instances. This manager therefore restricts itself to one managed Player process and blocks a launch if any Roblox Player process is already running. It does not alter mutexes, client binaries, anti-cheat, or integrity checks. Two independent authenticated instances were not experimentally launched; multi-instance support is not claimed.

