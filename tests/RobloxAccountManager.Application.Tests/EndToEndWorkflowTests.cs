using FluentAssertions;
using Moq;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;
using RobloxAccountManager.Core.Security;
using Xunit;

namespace RobloxAccountManager.Application.Tests;

public sealed class EndToEndWorkflowTests
{
    private static readonly DateTime ProcessStartedAt = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
    private const int ProcessId = 5566;
    private const string ExecutablePath = @"C:\Program Files (x86)\Roblox\Versions\v1\RobloxPlayerBeta.exe";

    [Fact]
    public async Task CompleteAccountLifecycle_RegistrationToEncryptionToSessionTracking_ExecutesCleanly()
    {
        // 1. Setup in-memory mock repositories and services
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            Username = "RobloxPlayerOne",
            DisplayName = "Player 1",
            Tag = "Main"
        };

        var accountsRepoMock = new Mock<IAccountRepository>();
        accountsRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // 2. Protect sensitive credential in memory via SensitiveBuffer
        var rawCredentials = "sample_session_secret_data"u8.ToArray();
        using (var sensitiveBuffer = new SensitiveBuffer(rawCredentials))
        {
            account.EncryptedSecret = (byte[])sensitiveBuffer.DangerousGetBuffer().Clone();
            account.EncryptedSecret.Should().NotBeEmpty();
        }

        // 3. Configure Roblox launcher & process inspection
        var launcherMock = new Mock<IRobloxLauncher>();
        launcherMock.Setup(l => l.FindPreferredInstallation())
            .Returns(new RobloxInstallation { ExecutablePath = ExecutablePath, IsSignatureVerified = true });
        launcherMock.Setup(l => l.LaunchAsync(It.IsAny<RobloxInstallation>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LaunchResult.Succeeded(ProcessId, ProcessStartedAt));

        var fakeSessions = new FakeSessionRepository();
        var fakeInspector = new FakeProcessInspector
        {
            Inspection = new ProcessInspectionResult(
                ProcessInspectionStatus.Running,
                new ClientProcessSnapshot(ProcessId, ProcessStartedAt, ExecutablePath))
        };

        var sessionService = new ClientSessionService(
            accountsRepoMock.Object,
            fakeSessions,
            fakeInspector,
            launcherMock.Object);

        // 4. Launch session
        var launchedSession = await sessionService.LaunchAsync(accountId);
        launchedSession.Should().NotBeNull();
        launchedSession.Status.Should().Be(ClientSessionStatus.Running);
        launchedSession.ProcessId.Should().Be(ProcessId);
        launchedSession.AccountUsername.Should().Be("RobloxPlayerOne");

        // 5. Query active sessions
        var sessions = await sessionService.RefreshAsync();
        sessions.Should().ContainSingle(s => s.Id == launchedSession.Id && s.IsActive);

        // 6. Inspect process exit and synchronize session
        fakeInspector.Inspection = new ProcessInspectionResult(ProcessInspectionStatus.Exited);
        var refreshedSessions = await sessionService.RefreshAsync();
        refreshedSessions.First(s => s.Id == launchedSession.Id).Status.Should().Be(ClientSessionStatus.Exited);
        refreshedSessions.Where(s => s.IsActive).Should().BeEmpty();
    }

    private sealed class FakeSessionRepository : IClientSessionRepository
    {
        public List<ClientSession> Sessions { get; } = new();

        public Task<IReadOnlyList<ClientSession>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ClientSession>>(Sessions.ToArray());

        public Task AddAsync(ClientSession session, CancellationToken cancellationToken = default)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ClientSession session, CancellationToken cancellationToken = default)
        {
            var index = Sessions.FindIndex(existing => existing.Id == session.Id);
            if (index >= 0)
            {
                Sessions[index] = session;
            }
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProcessInspector : IClientProcessInspector
    {
        public ProcessInspectionResult Inspection { get; set; } = new(ProcessInspectionStatus.Exited);

        public ProcessInspectionResult Inspect(int processId) => Inspection;
        public int CountRunningRobloxPlayers() => Inspection.Status == ProcessInspectionStatus.Running ? 1 : 0;
        public bool HasUnmanagedRobloxPlayer(IReadOnlyCollection<int> managedProcessIds) => false;
        public bool Terminate(ClientProcessSnapshot expectedProcess)
        {
            Inspection = new ProcessInspectionResult(ProcessInspectionStatus.Exited);
            return true;
        }
    }
}
