using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using RobloxAccountManager.Infrastructure.Database;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests;

public sealed class DatabaseInitializerRecoveryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ram_db_recovery_{Guid.NewGuid():N}");

    [Fact]
    public async Task InitializeAsync_CorruptDatabaseFailsWithoutReplacingOriginalFile()
    {
        Directory.CreateDirectory(_directory);
        var databasePath = Path.Combine(_directory, "vault.db");
        var originalBytes = System.Text.Encoding.UTF8.GetBytes("not a sqlite database");
        await File.WriteAllBytesAsync(databasePath, originalBytes);
        var initializer = new DatabaseInitializer(databasePath, NullLogger<DatabaseInitializer>.Instance);

        var act = () => initializer.InitializeAsync();

        await act.Should().ThrowAsync<SqliteException>();
        SqliteConnection.ClearAllPools();
        (await File.ReadAllBytesAsync(databasePath)).Should().Equal(originalBytes);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}