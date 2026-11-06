using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RobloxAccountManager.Core.Models;
using RobloxAccountManager.Core.Security;
using RobloxAccountManager.Infrastructure.Database;
using RobloxAccountManager.Infrastructure.Security;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests;

public class CredentialVaultTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DatabaseInitializer _initializer;
    private readonly SqliteAccountRepository _repository;
    private readonly DpapiEncryptionService _encryptionService;
    private readonly CredentialVault _vault;

    public CredentialVaultTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"ram_vault_test_{Guid.NewGuid():N}.db");
        _initializer = new DatabaseInitializer(_testDbPath, NullLogger<DatabaseInitializer>.Instance);
        _initializer.InitializeAsync().GetAwaiter().GetResult();
        _repository = new SqliteAccountRepository(_initializer, NullLogger<SqliteAccountRepository>.Instance);
        _encryptionService = new DpapiEncryptionService();
        _vault = new CredentialVault(_repository, _encryptionService, NullLogger<CredentialVault>.Instance);
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
            // Cleanup
        }
    }

    [Fact]
    public async Task StoreAndRetrieveSecretAsync_PersistsEncryptedBlob_AndRetrievesPlaintext()
    {
        var account = new Account
        {
            Username = "VaultUser1"
        };
        await _repository.AddAsync(account);

        var secretBytes = Encoding.UTF8.GetBytes("MockSecretSessionCookieValue_12345");
        using (var buffer = new SensitiveBuffer(secretBytes))
        {
            await _vault.StoreSecretAsync(account.Id, buffer);
        }

        // Verify stored in DB as ciphertext, NOT plaintext
        var rawAccountInDb = await _repository.GetByIdAsync(account.Id);
        rawAccountInDb!.EncryptedSecret.Should().NotBeEmpty();
        rawAccountInDb.EncryptedSecret.Should().NotEqual(secretBytes);
        rawAccountInDb.SecretEntropy.Should().NotBeNull();

        // Retrieve and verify plaintext match
        using (var retrieved = await _vault.RetrieveSecretAsync(account.Id))
        {
            retrieved.Should().NotBeNull();
            retrieved!.DangerousGetBuffer().Should().Equal(secretBytes);
        }
    }

    [Fact]
    public async Task ClearSecretAsync_WipesEncryptedSecretFromDatabase()
    {
        var account = new Account
        {
            Username = "VaultUser2"
        };
        await _repository.AddAsync(account);

        var secretBytes = Encoding.UTF8.GetBytes("TemporarySecret");
        using (var buffer = new SensitiveBuffer(secretBytes))
        {
            await _vault.StoreSecretAsync(account.Id, buffer);
        }

        await _vault.ClearSecretAsync(account.Id);

        var accountAfterClear = await _repository.GetByIdAsync(account.Id);
        accountAfterClear!.EncryptedSecret.Should().BeEmpty();
        accountAfterClear.SecretEntropy.Should().BeNull();

        using var retrieved = await _vault.RetrieveSecretAsync(account.Id);
        retrieved.Should().BeNull();
    }
}
