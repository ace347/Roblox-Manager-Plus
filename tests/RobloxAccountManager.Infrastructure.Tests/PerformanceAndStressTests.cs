using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using RobloxAccountManager.Core.Models;
using RobloxAccountManager.Core.Security;
using RobloxAccountManager.Infrastructure.Database;
using RobloxAccountManager.Infrastructure.Security;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests;

public sealed class PerformanceAndStressTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DatabaseInitializer _initializer;
    private readonly SqliteAccountRepository _repository;

    public PerformanceAndStressTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"ram_perf_{Guid.NewGuid():N}.db");
        _initializer = new DatabaseInitializer(_testDbPath, NullLogger<DatabaseInitializer>.Instance);
        _initializer.InitializeAsync().GetAwaiter().GetResult();
        _repository = new SqliteAccountRepository(_initializer, NullLogger<SqliteAccountRepository>.Instance);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
            var shm = $"{_testDbPath}-shm";
            var wal = $"{_testDbPath}-wal";
            if (File.Exists(shm)) File.Delete(shm);
            if (File.Exists(wal)) File.Delete(wal);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    [Fact]
    public async Task BulkInsertAndQueryPerformance_UnderWalMode_CompletesWithinStrictSla()
    {
        const int accountCount = 500;
        var stopwatch = Stopwatch.StartNew();

        // Act - Bulk Insert 500 accounts
        for (int i = 0; i < accountCount; i++)
        {
            var account = new Account
            {
                Username = $"User_{i:D4}",
                DisplayName = $"Display {i}",
                Tag = i % 2 == 0 ? "Main" : "Alt",
                Notes = "Benchmark synthetic record"
            };

            await _repository.AddAsync(account);
        }

        var insertElapsedMs = stopwatch.ElapsedMilliseconds;

        // Act - Search & Query across 500 accounts
        stopwatch.Restart();
        var allAccounts = await _repository.GetAllAsync();
        var searchResults = await _repository.SearchAsync("User_0250");
        var queryElapsedMs = stopwatch.ElapsedMilliseconds;

        // Assert SLA:
        // Bulk insertion of 500 records with WAL should complete comfortably under 5000ms.
        insertElapsedMs.Should().BeLessThan(5000, "500 account inserts should complete comfortably within 5 seconds");
        allAccounts.Should().HaveCount(accountCount);

        // Search & read queries must execute in under 250 ms
        queryElapsedMs.Should().BeLessThan(250, "Indexed read and keyword search queries must execute under 250ms");
        searchResults.Should().ContainSingle();
        searchResults[0].Username.Should().Be("User_0250");
    }

    [Fact]
    public void SensitiveBufferAndDpapiEncryption_HighFrequencyThroughput_ExecutesZeroLeakClean()
    {
        var entropy = Encoding.UTF8.GetBytes("perf_entropy_12345");
        var service = new DpapiEncryptionService();
        const int iterations = 200;

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var plaintext = Encoding.UTF8.GetBytes($"session_cookie_token_payload_sample_data_{i}");
            using var buffer = new SensitiveBuffer(plaintext);

            // Encrypt using underlying DPAPI
            var encrypted = service.Encrypt(buffer.DangerousGetBuffer(), entropy);
            encrypted.Should().NotBeEmpty();

            // Decrypt
            var decrypted = service.Decrypt(encrypted, entropy);
            decrypted.Length.Should().Be(plaintext.Length);
            CryptographicOperations.ZeroMemory(decrypted);
        }

        stopwatch.Stop();

        // 200 encryption + decryption roundtrips via DPAPI should complete in under 2500ms
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2500, "200 DPAPI encrypt/decrypt cycles should complete in under 2.5s");
    }

    [Fact]
    public async Task ConcurrentReadWriteStress_UnderWalMode_MaintainsDatabaseConsistency()
    {
        const int taskCount = 10;
        const int iterationsPerTask = 20;

        // Act - Run concurrent reads and writes across multiple tasks
        var tasks = Enumerable.Range(0, taskCount).Select(async taskId =>
        {
            var localRepo = new SqliteAccountRepository(_initializer, NullLogger<SqliteAccountRepository>.Instance);
            for (int i = 0; i < iterationsPerTask; i++)
            {
                var account = new Account
                {
                    Username = $"ConcUser_{taskId}_{i}",
                    DisplayName = $"Concurrent User {taskId}-{i}",
                    Tag = "StressTest"
                };

                await localRepo.AddAsync(account);

                // Immediate read
                var fetched = await localRepo.GetByUsernameAsync($"ConcUser_{taskId}_{i}");
                fetched.Should().NotBeNull();
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        var total = await _repository.GetAllAsync();
        total.Should().HaveCount(taskCount * iterationsPerTask);
    }
}
