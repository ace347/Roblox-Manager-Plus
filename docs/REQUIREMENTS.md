# Requirements Specification

This document defines functional (FR), non-functional (NFR), security (SEC), and privacy (PRIV) requirements.

---

## 1. Functional Requirements (FR)

| ID | Title | Description | Priority |
|---|---|---|---|
| **FR-001** | Add Account | Allow user to add an account specifying username, optional alias, and category tag. | High |
| **FR-002** | Secure Credential Store | Store account session tokens securely using OS-backed encryption. | Critical |
| **FR-003** | List & Filter Accounts | Display account cards with real-time search filtering by username and tags. | High |
| **FR-004** | Launch Account | Launch the official Roblox client for the chosen account safely without shell prompts. | High |
| **FR-005** | Default Account | Support setting a preferred default account for quick launches. | Medium |
| **FR-006** | Avatar Display | Fetch public thumbnail image for the account using public Roblox avatar web API. | Medium |
| **FR-007** | Account Deletion | Completely purge account records and encrypted blobs from SQLite on user request. | High |
| **FR-008** | Backup & Restore | Export encrypted vault using a user-specified passphrase (AES-256-GCM + PBKDF2/Argon2). | Low |

---

## 2. Security Requirements (SEC)

| ID | Title | Description | Priority |
|---|---|---|---|
| **SEC-001** | DPAPI Encryption | Sensitive credentials must be encrypted using Windows DPAPI scoped to `CurrentUser`. | Critical |
| **SEC-002** | No Plaintext Keys | No cryptographic keys hardcoded in assemblies or config files. | Critical |
| **SEC-003** | Parameterized SQL | All SQLite operations must use strictly parameterized queries. | Critical |
| **SEC-004** | Safe Process Launch | Process execution must supply argument lists directly without invoking `cmd.exe` or `powershell.exe`. | Critical |
| **SEC-005** | Sensitive Log Masking | Logs must redact tokens matching session or credential patterns. | High |
| **SEC-006** | In-Memory Clearance | Clear sensitive buffers (`byte[]`) in memory immediately after handoff. | High |

---

## 3. Privacy Requirements (PRIV)

| ID | Title | Description | Priority |
|---|---|---|---|
| **PRIV-001** | Zero External Telemetry | The application must not transmit usage statistics, error logs, or analytics to external servers. | Critical |
| **PRIV-002** | Local Storage Only | Database and preferences must reside exclusively in local user application directories. | Critical |
| **PRIV-003** | Transparent Networking | Network requests must only occur for documented user actions (such as avatar thumbnail retrieval). | High |

---

## 4. Performance Requirements (PERF)

| ID | Title | Description | Priority |
|---|---|---|---|
| **PERF-001** | Rapid Startup | Cold start time to interactive dashboard under 1.5 seconds. | High |
| **PERF-002** | UI Virtualization | Account list must use WPF item virtualization (`VirtualizingStackPanel`) to maintain 60 FPS scrolling. | High |
| **PERF-003** | Idle Memory Footprint | Memory consumption while idling in background should remain under 60 MB. | Medium |
