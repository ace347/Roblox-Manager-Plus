using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace RobloxAccountManager.Infrastructure.Database;

public sealed class DatabaseInitializer
{
    private const int LatestSchemaVersion = 2;
    private readonly string _connectionString;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(string dbPath, ILogger<DatabaseInitializer> logger)
    {
        _logger = logger;
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true
        };
        _connectionString = builder.ConnectionString;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing and running database migrations.");
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using (var pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA journal_mode = WAL;";
            var journalMode = await pragmaCmd.ExecuteScalarAsync(cancellationToken);
            if (!string.Equals(journalMode?.ToString(), "wal", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("SQLite could not enable WAL journal mode.");
            }
        }

        using (var pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
            await pragmaCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        using var transaction = connection.BeginTransaction();
        try
        {
            // Create schema version tracker
            using (var versionCmd = connection.CreateCommand())
            {
                versionCmd.Transaction = transaction;
                versionCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SchemaMigrations (
                        Version INTEGER PRIMARY KEY NOT NULL,
                        AppliedAtUtc TEXT NOT NULL
                    );
                ";
                await versionCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            int currentVersion;
            using (var currentVersionCmd = connection.CreateCommand())
            {
                currentVersionCmd.Transaction = transaction;
                currentVersionCmd.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM SchemaMigrations;";
                currentVersion = Convert.ToInt32(await currentVersionCmd.ExecuteScalarAsync(cancellationToken));
            }

            if (currentVersion > LatestSchemaVersion)
            {
                throw new NotSupportedException(
                    $"Database schema version {currentVersion} is newer than the supported version {LatestSchemaVersion}.");
            }

            if (currentVersion < 1)
            {
                _logger.LogInformation("Applying migration 1: Initial schema.");
                using var migCmd = connection.CreateCommand();
                migCmd.Transaction = transaction;
                migCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Accounts (
                        Id TEXT PRIMARY KEY NOT NULL,
                        RobloxUserId INTEGER NULL,
                        Username TEXT NOT NULL COLLATE NOCASE,
                        DisplayName TEXT NULL,
                        Tag TEXT NOT NULL DEFAULT 'Main',
                        Notes TEXT NULL,
                        AvatarUrl TEXT NULL,
                        IsDefault INTEGER NOT NULL DEFAULT 0,
                        EncryptedSecret BLOB NOT NULL,
                        SecretEntropy BLOB NULL,
                        LastUsedAtUtc TEXT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS UQ_Accounts_Username ON Accounts(Username);
                    CREATE INDEX IF NOT EXISTS IX_Accounts_Tag ON Accounts(Tag);
                    CREATE INDEX IF NOT EXISTS IX_Accounts_LastUsedAtUtc ON Accounts(LastUsedAtUtc);

                    CREATE TABLE IF NOT EXISTS AppSettings (
                        Key TEXT PRIMARY KEY NOT NULL,
                        Value TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL
                    );

                    INSERT INTO SchemaMigrations (Version, AppliedAtUtc) VALUES (1, $now);
                ";
                migCmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
                await migCmd.ExecuteNonQueryAsync(cancellationToken);
                currentVersion = 1;
            }

            if (currentVersion < 2)
            {
                _logger.LogInformation("Applying migration 2: Managed client launch history.");
                using var migrationCmd = connection.CreateCommand();
                migrationCmd.Transaction = transaction;
                migrationCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ClientSessions (
                        Id TEXT PRIMARY KEY NOT NULL,
                        AccountId TEXT NOT NULL,
                        AccountUsername TEXT NOT NULL,
                        AccountDisplayName TEXT NULL,
                        ProcessId INTEGER NULL,
                        ProcessStartTimeUtc TEXT NULL,
                        ExecutablePath TEXT NOT NULL,
                        LaunchedAtUtc TEXT NOT NULL,
                        Status TEXT NOT NULL CHECK (Status IN ('Launching', 'Running', 'StatusUnavailable', 'Exited', 'LaunchFailed')),
                        EndedAtUtc TEXT NULL,
                        LastError TEXT NULL,
                        ActiveSlot INTEGER NOT NULL DEFAULT 0 CHECK (ActiveSlot IN (0, 1))
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS UQ_ClientSessions_ActiveSlot
                        ON ClientSessions(ActiveSlot) WHERE ActiveSlot = 1;
                    CREATE INDEX IF NOT EXISTS IX_ClientSessions_AccountId ON ClientSessions(AccountId);
                    CREATE INDEX IF NOT EXISTS IX_ClientSessions_LaunchedAtUtc ON ClientSessions(LaunchedAtUtc);

                    INSERT INTO SchemaMigrations (Version, AppliedAtUtc) VALUES (2, $now);
                ";
                migrationCmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
                await migrationCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Check database integrity
            using (var integrityCmd = connection.CreateCommand())
            {
                integrityCmd.Transaction = transaction;
                integrityCmd.CommandText = "PRAGMA integrity_check;";
                var integrityResult = await integrityCmd.ExecuteScalarAsync(cancellationToken);
                if (integrityResult?.ToString() != "ok")
                {
                    throw new InvalidOperationException($"SQLite integrity check failed: {integrityResult}");
                }
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Database migrations completed and integrity verified.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed, rolling back.");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}
