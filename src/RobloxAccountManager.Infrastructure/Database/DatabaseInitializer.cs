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
            Mode = SqliteOpenMode.ReadWriteCreate
        };
        _connectionString = builder.ConnectionString;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing SQLite database.");
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Accounts (
                Id TEXT PRIMARY KEY NOT NULL,
                RobloxUserId INTEGER NULL,
                Username TEXT NOT NULL,
                DisplayName TEXT NULL,
                EncryptedSecret BLOB NOT NULL,
                SecretEntropy BLOB NULL,
                AvatarUrl TEXT NULL,
                Notes TEXT NULL,
                IsDefault INTEGER NOT NULL DEFAULT 0,
                LastUsedAtUtc TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Accounts_Username ON Accounts(Username);
            CREATE INDEX IF NOT EXISTS IX_Accounts_LastUsedAtUtc ON Accounts(LastUsedAtUtc);

            CREATE TABLE IF NOT EXISTS AppSettings (
                Key TEXT PRIMARY KEY NOT NULL,
                Value TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );
        ";

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("SQLite database schema verified successfully.");
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}
