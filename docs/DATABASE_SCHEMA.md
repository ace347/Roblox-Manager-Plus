# Database Schema & Persistence Strategy

## 1. Technology & Connection Strategy

- **Engine:** SQLite (via `Microsoft.Data.Sqlite`)
- **Location:** `%LOCALAPPDATA%\SecureRobloxAccountManager\vault.db`
- **Journal Mode:** WAL (Write-Ahead Logging) for superior concurrent read performance and crash resilience.
- **Foreign Keys:** Enabled explicitly (`PRAGMA foreign_keys = ON;`).

---

## 2. Relational Schema

```sql
CREATE TABLE IF NOT EXISTS Accounts (
    Id TEXT PRIMARY KEY NOT NULL,                -- GUID string
    RobloxUserId INTEGER NULL,                   -- Official numeric user ID
    Username TEXT NOT NULL,                      -- Primary Roblox username
    DisplayName TEXT NULL,                       -- Display name alias
    EncryptedSecret BLOB NOT NULL,               -- DPAPI-encrypted token or secret
    SecretEntropy BLOB NULL,                     -- Optional per-record salt/entropy
    AvatarUrl TEXT NULL,                         -- Public thumbnail URL
    Notes TEXT NULL,                             -- User-specified private notes
    IsDefault INTEGER NOT NULL DEFAULT 0,        -- 1 = Default account, 0 = Standard
    LastUsedAtUtc TEXT NULL,                     -- ISO-8601 UTC timestamp
    CreatedAtUtc TEXT NOT NULL,                  -- ISO-8601 UTC timestamp
    UpdatedAtUtc TEXT NOT NULL                   -- ISO-8601 UTC timestamp
);

CREATE INDEX IF NOT EXISTS IX_Accounts_Username ON Accounts(Username);
CREATE INDEX IF NOT EXISTS IX_Accounts_LastUsedAtUtc ON Accounts(LastUsedAtUtc);

CREATE TABLE IF NOT EXISTS AppSettings (
    Key TEXT PRIMARY KEY NOT NULL,
    Value TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);
```

---

## 3. Migration Strategy

- Migrations are versioned sequentially (`001_initial_schema.sql`, etc.) and applied inside a single database transaction upon application startup.
- All column mutations and table upgrades run idempotently.
