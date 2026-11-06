using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
