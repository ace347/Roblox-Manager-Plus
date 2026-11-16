# Database Schema & Persistence Strategy

## 1. Technology & Connection Strategy

- **Engine:** SQLite (via `Microsoft.Data.Sqlite`)
- **Location:** `%LOCALAPPDATA%\RobloxAccountManager\vault.db` (as currently configured by `App`).
- **Journal Mode:** WAL (Write-Ahead Logging) for superior concurrent read performance and crash resilience.
- **Foreign Keys:** Enabled in the SQLite connection string for every connection.

---

## 2. Relational Schema & Versioned Migrations

Database versioning is tracked via the `SchemaMigrations` table:

```sql
CREATE TABLE IF NOT EXISTS SchemaMigrations (
    Version INTEGER PRIMARY KEY NOT NULL,
    AppliedAtUtc TEXT NOT NULL
);
```

### Migration 1: Accounts & AppSettings (Implemented)

```sql
CREATE TABLE IF NOT EXISTS Accounts (
    Id TEXT PRIMARY KEY NOT NULL,                -- GUID string
    RobloxUserId INTEGER NULL,                   -- Official numeric user ID
    Username TEXT NOT NULL COLLATE NOCASE,       -- Unique case-insensitive username
    DisplayName TEXT NULL,                       -- Display name alias
    Tag TEXT NOT NULL DEFAULT 'Main',            -- Profile category tag
    Notes TEXT NULL,                             -- Personal notes
    AvatarUrl TEXT NULL,                         -- Public thumbnail URL
    IsDefault INTEGER NOT NULL DEFAULT 0,        -- 1 = Default account, 0 = Standard
    EncryptedSecret BLOB NOT NULL,               -- DPAPI-encrypted token or secret
    SecretEntropy BLOB NULL,                     -- Optional per-record salt/entropy
    LastUsedAtUtc TEXT NULL,                     -- ISO-8601 UTC timestamp
    CreatedAtUtc TEXT NOT NULL,                  -- ISO-8601 UTC timestamp
    UpdatedAtUtc TEXT NOT NULL                   -- ISO-8601 UTC timestamp
);

CREATE UNIQUE INDEX IF NOT EXISTS UQ_Accounts_Username ON Accounts(Username);
CREATE INDEX IF NOT EXISTS IX_Accounts_Tag ON Accounts(Tag);
CREATE INDEX IF NOT EXISTS IX_Accounts_LastUsedAtUtc ON Accounts(LastUsedAtUtc);

CREATE TABLE IF NOT EXISTS AppSettings (
    Key TEXT PRIMARY KEY NOT NULL,
    Value TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);
```

---

## 3. Migration Strategy

- Migration versions are applied in ascending order inside one transaction and recorded in `SchemaMigrations`.
- WAL mode is configured before the migration transaction; changing journal mode inside a transaction would not take effect.
- Migration 1 creates `Accounts` and `AppSettings`.
- Migration 2 creates `ClientSessions` for process-launch history. Unknown future schema versions are rejected without recreating the database. Startup integrity checking detects some corruption, but there is no automated repair or backup/restore flow.
- The UI creates accounts with an empty `EncryptedSecret`; the independent `CredentialVault` can store ciphertext, but no credential-entry workflow currently calls it.

### Migration 2: Managed client launch history

```sql
CREATE TABLE ClientSessions (
    Id TEXT PRIMARY KEY NOT NULL,
    AccountId TEXT NOT NULL,
    AccountUsername TEXT NOT NULL,
    AccountDisplayName TEXT NULL,
    ProcessId INTEGER NULL,
    ProcessStartTimeUtc TEXT NULL,
    ExecutablePath TEXT NOT NULL,
    LaunchedAtUtc TEXT NOT NULL,
    Status TEXT NOT NULL,
    EndedAtUtc TEXT NULL,
    LastError TEXT NULL,
    ActiveSlot INTEGER NOT NULL DEFAULT 0 CHECK (ActiveSlot IN (0, 1))
);

CREATE UNIQUE INDEX UQ_ClientSessions_ActiveSlot
    ON ClientSessions(ActiveSlot) WHERE ActiveSlot = 1;
```

The account ID and username/display-name snapshot are retained in launch history without a cascading foreign key, so deleting a profile does not erase session history. No authentication tokens or credentials are stored in this table. The unique active slot prevents multiple RAM Plus managed sessions, including concurrent launch attempts from separate app processes.

## 4. Deletion and File Protection

- Account deletion removes the logical SQLite row. Secret clearing replaces the encrypted blob with an empty value. Neither operation guarantees physical erasure from SQLite pages, WAL files, backups, or storage-device remapping.
- The database is placed under the current user's Local Application Data directory. The application does not explicitly set or audit Windows ACLs; protection relies on inherited directory permissions.

