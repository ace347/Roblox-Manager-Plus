using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace RobloxAccountManager.Infrastructure.Database;

public sealed class DatabaseInitializer
{
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
            Cache = SqliteCacheMode.Shared
        };
        _connectionString = builder.ConnectionString;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing and running database migrations.");
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            // Configure pragmas
            using (var pragmaCmd = connection.CreateCommand())
            {
                pragmaCmd.Transaction = transaction;
                pragmaCmd.CommandText = @"
                    PRAGMA journal_mode = WAL;
                    PRAGMA foreign_keys = ON;
                ";
                await pragmaCmd.ExecuteNonQueryAsync(cancellationToken);
            }

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

            // Migration 1: Base Accounts & AppSettings
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version = 1;";
                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken));

                if (count == 0)
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
                }
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
