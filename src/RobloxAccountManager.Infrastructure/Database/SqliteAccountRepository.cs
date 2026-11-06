using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Infrastructure.Database;

public sealed class SqliteAccountRepository : IAccountRepository
{
    private readonly DatabaseInitializer _db;
    private readonly ILogger<SqliteAccountRepository> _logger;

    public SqliteAccountRepository(DatabaseInitializer db, ILogger<SqliteAccountRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, RobloxUserId, Username, DisplayName, Tag, Notes, AvatarUrl,
                   IsDefault, EncryptedSecret, SecretEntropy, LastUsedAtUtc, CreatedAtUtc, UpdatedAtUtc
            FROM Accounts
            ORDER BY IsDefault DESC, LastUsedAtUtc DESC, CreatedAtUtc DESC;
        ";

        var list = new List<Account>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapFromReader(reader));
        }

        return list;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, RobloxUserId, Username, DisplayName, Tag, Notes, AvatarUrl,
                   IsDefault, EncryptedSecret, SecretEntropy, LastUsedAtUtc, CreatedAtUtc, UpdatedAtUtc
            FROM Accounts
            WHERE Id = $id;
        ";
        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapFromReader(reader);
        }

        return null;
    }

    public async Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, RobloxUserId, Username, DisplayName, Tag, Notes, AvatarUrl,
                   IsDefault, EncryptedSecret, SecretEntropy, LastUsedAtUtc, CreatedAtUtc, UpdatedAtUtc
            FROM Accounts
            WHERE Username = $username;
        ";
        command.Parameters.AddWithValue("$username", username);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapFromReader(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<Account>> SearchAsync(string searchTerm, string? tag = null, CancellationToken cancellationToken = default)
    {
        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        var sql = @"
            SELECT Id, RobloxUserId, Username, DisplayName, Tag, Notes, AvatarUrl,
                   IsDefault, EncryptedSecret, SecretEntropy, LastUsedAtUtc, CreatedAtUtc, UpdatedAtUtc
            FROM Accounts
            WHERE (Username LIKE $term OR DisplayName LIKE $term OR Notes LIKE $term)
        ";

        command.Parameters.AddWithValue("$term", $"%{searchTerm}%");

        if (!string.IsNullOrWhiteSpace(tag))
        {
            sql += " AND Tag = $tag";
            command.Parameters.AddWithValue("$tag", tag);
        }

        sql += " ORDER BY IsDefault DESC, LastUsedAtUtc DESC;";
        command.CommandText = sql;

        var list = new List<Account>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapFromReader(reader));
        }

        return list;
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            if (account.IsDefault)
            {
                using var unsetCmd = connection.CreateCommand();
                unsetCmd.Transaction = transaction;
                unsetCmd.CommandText = "UPDATE Accounts SET IsDefault = 0;";
                await unsetCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO Accounts (
                    Id, RobloxUserId, Username, DisplayName, Tag, Notes, AvatarUrl,
                    IsDefault, EncryptedSecret, SecretEntropy, LastUsedAtUtc, CreatedAtUtc, UpdatedAtUtc
                ) VALUES (
                    $id, $robloxUserId, $username, $displayName, $tag, $notes, $avatarUrl,
                    $isDefault, $encryptedSecret, $secretEntropy, $lastUsedAtUtc, $createdAtUtc, $updatedAtUtc
                );
            ";

            command.Parameters.AddWithValue("$id", account.Id.ToString());
            command.Parameters.AddWithValue("$robloxUserId", (object?)account.RobloxUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("$username", account.Username);
            command.Parameters.AddWithValue("$displayName", (object?)account.DisplayName ?? DBNull.Value);
            command.Parameters.AddWithValue("$tag", (object?)account.Tag ?? "Main");
            command.Parameters.AddWithValue("$notes", (object?)account.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("$avatarUrl", (object?)account.AvatarUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("$isDefault", account.IsDefault ? 1 : 0);
            command.Parameters.AddWithValue("$encryptedSecret", account.EncryptedSecret);
            command.Parameters.AddWithValue("$secretEntropy", (object?)account.SecretEntropy ?? DBNull.Value);
            command.Parameters.AddWithValue("$lastUsedAtUtc", account.LastUsedAtUtc.HasValue ? account.LastUsedAtUtc.Value.ToString("o") : (object)DBNull.Value);
            command.Parameters.AddWithValue("$createdAtUtc", account.CreatedAtUtc.ToString("o"));
            command.Parameters.AddWithValue("$updatedAtUtc", account.UpdatedAtUtc.ToString("o"));

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Account added: {Username}", account.Username);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            if (account.IsDefault)
            {
                using var unsetCmd = connection.CreateCommand();
                unsetCmd.Transaction = transaction;
                unsetCmd.CommandText = "UPDATE Accounts SET IsDefault = 0 WHERE Id != $id;";
                unsetCmd.Parameters.AddWithValue("$id", account.Id.ToString());
                await unsetCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Accounts SET
                    RobloxUserId = $robloxUserId,
                    Username = $username,
                    DisplayName = $displayName,
                    Tag = $tag,
                    Notes = $notes,
                    AvatarUrl = $avatarUrl,
                    IsDefault = $isDefault,
                    EncryptedSecret = $encryptedSecret,
                    SecretEntropy = $secretEntropy,
                    LastUsedAtUtc = $lastUsedAtUtc,
                    UpdatedAtUtc = $updatedAtUtc
                WHERE Id = $id;
            ";

            command.Parameters.AddWithValue("$id", account.Id.ToString());
            command.Parameters.AddWithValue("$robloxUserId", (object?)account.RobloxUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("$username", account.Username);
            command.Parameters.AddWithValue("$displayName", (object?)account.DisplayName ?? DBNull.Value);
            command.Parameters.AddWithValue("$tag", (object?)account.Tag ?? "Main");
            command.Parameters.AddWithValue("$notes", (object?)account.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("$avatarUrl", (object?)account.AvatarUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("$isDefault", account.IsDefault ? 1 : 0);
            command.Parameters.AddWithValue("$encryptedSecret", account.EncryptedSecret);
            command.Parameters.AddWithValue("$secretEntropy", (object?)account.SecretEntropy ?? DBNull.Value);
            command.Parameters.AddWithValue("$lastUsedAtUtc", account.LastUsedAtUtc.HasValue ? account.LastUsedAtUtc.Value.ToString("o") : (object)DBNull.Value);
            command.Parameters.AddWithValue("$updatedAtUtc", DateTime.UtcNow.ToString("o"));

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Account updated: {Username}", account.Username);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Accounts WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Account deleted: {Id}", id);
    }

    public async Task SetDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            using var clearCmd = connection.CreateCommand();
            clearCmd.Transaction = transaction;
            clearCmd.CommandText = "UPDATE Accounts SET IsDefault = 0;";
            await clearCmd.ExecuteNonQueryAsync(cancellationToken);

            using var setCmd = connection.CreateCommand();
            setCmd.Transaction = transaction;
            setCmd.CommandText = "UPDATE Accounts SET IsDefault = 1, UpdatedAtUtc = $now WHERE Id = $id;";
            setCmd.Parameters.AddWithValue("$id", id.ToString());
            setCmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
            await setCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static Account MapFromReader(SqliteDataReader reader)
    {
        return new Account
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
            RobloxUserId = reader.IsDBNull(reader.GetOrdinal("RobloxUserId")) ? null : reader.GetInt64(reader.GetOrdinal("RobloxUserId")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            DisplayName = reader.IsDBNull(reader.GetOrdinal("DisplayName")) ? null : reader.GetString(reader.GetOrdinal("DisplayName")),
            Tag = reader.IsDBNull(reader.GetOrdinal("Tag")) ? "Main" : reader.GetString(reader.GetOrdinal("Tag")),
            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
            AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? null : reader.GetString(reader.GetOrdinal("AvatarUrl")),
            IsDefault = reader.GetInt32(reader.GetOrdinal("IsDefault")) == 1,
            EncryptedSecret = (byte[])reader["EncryptedSecret"],
            SecretEntropy = reader.IsDBNull(reader.GetOrdinal("SecretEntropy")) ? null : (byte[])reader["SecretEntropy"],
            LastUsedAtUtc = reader.IsDBNull(reader.GetOrdinal("LastUsedAtUtc")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("LastUsedAtUtc"))),
            CreatedAtUtc = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAtUtc"))),
            UpdatedAtUtc = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAtUtc")))
        };
    }
}
