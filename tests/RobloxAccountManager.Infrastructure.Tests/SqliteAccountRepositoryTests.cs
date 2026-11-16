using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RobloxAccountManager.Core.Exceptions;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;
using RobloxAccountManager.Infrastructure.Database;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests;

public class SqliteAccountRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DatabaseInitializer _initializer;
    private readonly SqliteAccountRepository _repository;

    public SqliteAccountRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"ram_test_{Guid.NewGuid():N}.db");
        _initializer = new DatabaseInitializer(_testDbPath, NullLogger<DatabaseInitializer>.Instance);
        _initializer.InitializeAsync().GetAwaiter().GetResult();
        _repository = new SqliteAccountRepository(_initializer, NullLogger<SqliteAccountRepository>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
            var wal = _testDbPath + "-wal";
            if (File.Exists(wal)) File.Delete(wal);
            var shm = _testDbPath + "-shm";
            if (File.Exists(shm)) File.Delete(shm);
        }
        catch
        {
            // Best effort cleanup in tests
        }
    }

    [Fact]
    public async Task AddAndGetByIdAsync_StoresAndRetrievesAccountSuccessfully()
    {
        var account = new Account
        {
            Username = "TestPlayer1",
            DisplayName = "Player One",
            Tag = "Main",
            Notes = "Testing notes",
            EncryptedSecret = new byte[] { 1, 2, 3 }
        };

        await _repository.AddAsync(account);

        var retrieved = await _repository.GetByIdAsync(account.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Username.Should().Be("TestPlayer1");
        retrieved.DisplayName.Should().Be("Player One");
        retrieved.Tag.Should().Be("Main");
        retrieved.EncryptedSecret.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task InitializeAsync_EnablesWalAndForeignKeysOnConnections()
    {
        using var connection = _initializer.CreateConnection();
        await connection.OpenAsync();

        using var journalModeCommand = connection.CreateCommand();
        journalModeCommand.CommandText = "PRAGMA journal_mode;";
        var journalMode = await journalModeCommand.ExecuteScalarAsync();

        using var foreignKeysCommand = connection.CreateCommand();
        foreignKeysCommand.CommandText = "PRAGMA foreign_keys;";
        var foreignKeys = await foreignKeysCommand.ExecuteScalarAsync();

        journalMode.Should().Be("wal");
        foreignKeys.Should().Be(1L);
    }

    [Fact]
    public async Task InitializeAsync_AppliesSessionHistoryMigration()
    {
        using var connection = _initializer.CreateConnection();
        await connection.OpenAsync();

        using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "SELECT MAX(Version) FROM SchemaMigrations;";
        var version = await versionCommand.ExecuteScalarAsync();

        using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'ClientSessions';";
        var sessionTableCount = await tableCommand.ExecuteScalarAsync();

        version.Should().Be(2L);
        sessionTableCount.Should().Be(1L);
    }

    [Fact]
    public async Task InitializeAsync_RejectsFutureSchemaWithoutChangingExistingData()
    {
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                   new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = _testDbPath }.ConnectionString))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM SchemaMigrations;
                INSERT INTO SchemaMigrations (Version, AppliedAtUtc) VALUES (99, 'future');
                CREATE TABLE PreserveMe (Value TEXT NOT NULL);
                INSERT INTO PreserveMe (Value) VALUES ('keep');
            ";
            await command.ExecuteNonQueryAsync();
        }

        var act = () => _initializer.InitializeAsync();

        await act.Should().ThrowAsync<NotSupportedException>();

        using var verificationConnection = _initializer.CreateConnection();
        await verificationConnection.OpenAsync();
        using var verifyCommand = verificationConnection.CreateCommand();
        verifyCommand.CommandText = "SELECT Value FROM PreserveMe;";
        (await verifyCommand.ExecuteScalarAsync()).Should().Be("keep");
    }

    [Fact]
    public async Task InitializeAsync_FailedSessionMigrationRollsBackVersionRecord()
    {
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                   new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = _testDbPath }.ConnectionString))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM SchemaMigrations WHERE Version = 2;
                DROP TABLE ClientSessions;
                CREATE TABLE ClientSessions (UnexpectedColumn TEXT NOT NULL);
            ";
            await command.ExecuteNonQueryAsync();
        }

        var act = () => _initializer.InitializeAsync();

        await act.Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();

        using var verificationConnection = _initializer.CreateConnection();
        await verificationConnection.OpenAsync();
        using var verifyCommand = verificationConnection.CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version = 2;";
        (await verifyCommand.ExecuteScalarAsync()).Should().Be(0L);
    }

    [Fact]
    public async Task ClientSessionRepository_PersistsLaunchAssociationAndProcessMetadata()
    {
        var sessionRepository = new SqliteClientSessionRepository(_initializer);
        var session = new ClientSession
        {
            AccountId = Guid.NewGuid(),
            AccountUsername = "SavedProfile",
            AccountDisplayName = "Profile Display",
            ProcessId = 4321,
            ProcessStartedAtUtc = DateTime.UtcNow,
            ExecutablePath = @"C:\Roblox\RobloxPlayerBeta.exe",
            Status = ClientSessionStatus.Running
        };

        await sessionRepository.AddAsync(session);

        var persisted = await sessionRepository.GetAllAsync();
        persisted.Should().ContainSingle();
        persisted[0].Id.Should().Be(session.Id);
        persisted[0].AccountId.Should().Be(session.AccountId);
        persisted[0].AccountUsername.Should().Be("SavedProfile");
        persisted[0].ProcessId.Should().Be(4321);
        persisted[0].ExecutablePath.Should().Be(session.ExecutablePath);
        persisted[0].Status.Should().Be(ClientSessionStatus.Running);
        persisted[0].AuthenticationStatus.Should().Contain("Unverified");
    }

    [Fact]
    public async Task ClientSessionRepository_EnforcesOnlyOneActiveManagedSession()
    {
        var sessionRepository = new SqliteClientSessionRepository(_initializer);
        var first = new ClientSession
        {
            AccountId = Guid.NewGuid(),
            AccountUsername = "FirstProfile",
            ExecutablePath = @"C:\Roblox\RobloxPlayerBeta.exe",
            Status = ClientSessionStatus.Launching
        };
        await sessionRepository.AddAsync(first);

        var second = new ClientSession
        {
            AccountId = Guid.NewGuid(),
            AccountUsername = "SecondProfile",
            ExecutablePath = @"C:\Roblox\RobloxPlayerBeta.exe",
            Status = ClientSessionStatus.Launching
        };

        var act = () => sessionRepository.AddAsync(second);

        await act.Should().ThrowAsync<ClientSessionConflictException>();
        first.Status = ClientSessionStatus.Exited;
        first.EndedAtUtc = DateTime.UtcNow;
        await sessionRepository.UpdateAsync(first);
        await sessionRepository.AddAsync(second);

        (await sessionRepository.GetAllAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingAccountFields()
    {
        var account = new Account
        {
            Username = "OldUsername",
            DisplayName = "OldName",
            Tag = "Alt"
        };
        await _repository.AddAsync(account);

        account.DisplayName = "UpdatedName";
        account.Tag = "Developer";
        await _repository.UpdateAsync(account);

        var updated = await _repository.GetByIdAsync(account.Id);
        updated!.DisplayName.Should().Be("UpdatedName");
        updated.Tag.Should().Be("Developer");
    }

    [Fact]
    public async Task DeleteAsync_RemovesAccountFromDatabase()
    {
        var account = new Account
        {
            Username = "ToDelete"
        };
        await _repository.AddAsync(account);

        await _repository.DeleteAsync(account.Id);

        var retrieved = await _repository.GetByIdAsync(account.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_FindsMatchesByTermAndTag()
    {
        await _repository.AddAsync(new Account { Username = "ShadowNinja", Tag = "Main" });
        await _repository.AddAsync(new Account { Username = "ShadowMage", Tag = "Alt" });
        await _repository.AddAsync(new Account { Username = "LightWarrior", Tag = "Main" });

        var resultsAllShadow = await _repository.SearchAsync("Shadow");
        resultsAllShadow.Should().HaveCount(2);

        var resultsFilteredByTag = await _repository.SearchAsync("Shadow", "Alt");
        resultsFilteredByTag.Should().HaveCount(1);
        resultsFilteredByTag[0].Username.Should().Be("ShadowMage");
    }

    [Fact]
    public async Task AddAsync_ConcurrentTemporaryDatabaseWritesPersistAllAccounts()
    {
        var accounts = Enumerable.Range(0, 12)
            .Select(index => new Account { Username = $"ConcurrentUser{index}" })
            .ToArray();

        await Task.WhenAll(accounts.Select(account => _repository.AddAsync(account)));

        (await _repository.GetAllAsync()).Should().HaveCount(accounts.Length);
    }

    [Fact]
    public async Task SetDefaultAsync_UnsetsPreviousDefault_AndSetsNewDefault()
    {
        var acc1 = new Account { Username = "Acc1", IsDefault = true };
        var acc2 = new Account { Username = "Acc2", IsDefault = false };

        await _repository.AddAsync(acc1);
        await _repository.AddAsync(acc2);

        await _repository.SetDefaultAsync(acc2.Id);

        var updated1 = await _repository.GetByIdAsync(acc1.Id);
        var updated2 = await _repository.GetByIdAsync(acc2.Id);

        updated1!.IsDefault.Should().BeFalse();
        updated2!.IsDefault.Should().BeTrue();
    }
}
