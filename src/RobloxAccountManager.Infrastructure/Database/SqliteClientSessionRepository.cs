using System.Globalization;
using Microsoft.Data.Sqlite;
using RobloxAccountManager.Core.Exceptions;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Infrastructure.Database;

public sealed class SqliteClientSessionRepository : IClientSessionRepository
{
    private readonly DatabaseInitializer _database;

    public SqliteClientSessionRepository(DatabaseInitializer database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<ClientSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, AccountId, AccountUsername, AccountDisplayName, ProcessId, ProcessStartTimeUtc,
                   ExecutablePath, LaunchedAtUtc, Status, EndedAtUtc, LastError
            FROM ClientSessions
            ORDER BY LaunchedAtUtc DESC;
        ";

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var sessions = new List<ClientSession>();
        while (await reader.ReadAsync(cancellationToken))
        {
            sessions.Add(MapFromReader(reader));
        }

        return sessions;
    }

    public async Task AddAsync(ClientSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ClientSessions (
                Id, AccountId, AccountUsername, AccountDisplayName, ProcessId, ProcessStartTimeUtc,
                ExecutablePath, LaunchedAtUtc, Status, EndedAtUtc, LastError, ActiveSlot
            ) VALUES (
                $id, $accountId, $username, $displayName, $processId, $processStartTimeUtc,
                $executablePath, $launchedAtUtc, $status, $endedAtUtc, $lastError, $activeSlot
            );
        ";
        AddParameters(command, session);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
        {
            throw new ClientSessionConflictException();
        }
    }

    public async Task UpdateAsync(ClientSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE ClientSessions SET
                ProcessId = $processId,
                ProcessStartTimeUtc = $processStartTimeUtc,
                ExecutablePath = $executablePath,
                Status = $status,
                EndedAtUtc = $endedAtUtc,
                LastError = $lastError,
                ActiveSlot = $activeSlot
            WHERE Id = $id;
        ";
        AddParameters(command, session);

        try
        {
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affectedRows == 0)
            {
                throw new KeyNotFoundException($"Client session '{session.Id}' no longer exists.");
            }
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
        {
            throw new ClientSessionConflictException();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ClientSessions WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearInactiveAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ClientSessions WHERE ActiveSlot = 0 OR Status IN ('Exited', 'LaunchFailed', 'StatusUnavailable');";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(SqliteCommand command, ClientSession session)
    {
        command.Parameters.AddWithValue("$id", session.Id.ToString());
        command.Parameters.AddWithValue("$accountId", session.AccountId.ToString());
        command.Parameters.AddWithValue("$username", session.AccountUsername);
        command.Parameters.AddWithValue("$displayName", (object?)session.AccountDisplayName ?? DBNull.Value);
        command.Parameters.AddWithValue("$processId", (object?)session.ProcessId ?? DBNull.Value);
        command.Parameters.AddWithValue("$processStartTimeUtc", session.ProcessStartedAtUtc.HasValue
            ? session.ProcessStartedAtUtc.Value.ToString("o", CultureInfo.InvariantCulture)
            : DBNull.Value);
        command.Parameters.AddWithValue("$executablePath", session.ExecutablePath);
        command.Parameters.AddWithValue("$launchedAtUtc", session.LaunchedAtUtc.ToString("o", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$status", session.Status.ToString());
        command.Parameters.AddWithValue("$endedAtUtc", session.EndedAtUtc.HasValue
            ? session.EndedAtUtc.Value.ToString("o", CultureInfo.InvariantCulture)
            : DBNull.Value);
        command.Parameters.AddWithValue("$lastError", (object?)session.LastError ?? DBNull.Value);
        command.Parameters.AddWithValue("$activeSlot", session.IsActive ? 1 : 0);
    }

    private static ClientSession MapFromReader(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
        AccountId = Guid.Parse(reader.GetString(reader.GetOrdinal("AccountId"))),
        AccountUsername = reader.GetString(reader.GetOrdinal("AccountUsername")),
        AccountDisplayName = reader.IsDBNull(reader.GetOrdinal("AccountDisplayName"))
            ? null
            : reader.GetString(reader.GetOrdinal("AccountDisplayName")),
        ProcessId = reader.IsDBNull(reader.GetOrdinal("ProcessId"))
            ? null
            : reader.GetInt32(reader.GetOrdinal("ProcessId")),
        ProcessStartedAtUtc = reader.IsDBNull(reader.GetOrdinal("ProcessStartTimeUtc"))
            ? null
            : DateTime.Parse(reader.GetString(reader.GetOrdinal("ProcessStartTimeUtc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        ExecutablePath = reader.GetString(reader.GetOrdinal("ExecutablePath")),
        LaunchedAtUtc = DateTime.Parse(reader.GetString(reader.GetOrdinal("LaunchedAtUtc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        Status = Enum.Parse<ClientSessionStatus>(reader.GetString(reader.GetOrdinal("Status")), ignoreCase: false),
        EndedAtUtc = reader.IsDBNull(reader.GetOrdinal("EndedAtUtc"))
            ? null
            : DateTime.Parse(reader.GetString(reader.GetOrdinal("EndedAtUtc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        LastError = reader.IsDBNull(reader.GetOrdinal("LastError"))
            ? null
            : reader.GetString(reader.GetOrdinal("LastError"))
    };
}