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

- **Detection Locations:**
  - Standard user path: `%LOCALAPPDATA%\Roblox\Versions\version-*\RobloxPlayerBeta.exe`
  - Machine install path: `%PROGRAMFILES(X86)%\Roblox\Versions\version-*\RobloxPlayerBeta.exe`
- **Execution Mechanism:**
  - Launched via .NET `System.Diagnostics.Process` with explicit argument collections.
  - Never invoked via shell interpreters (`cmd.exe /c` or `powershell.exe`).
  - No DLL injection, memory patching, or API hooking.
