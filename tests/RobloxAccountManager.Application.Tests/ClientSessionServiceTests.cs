using FluentAssertions;
using Moq;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Exceptions;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.Application.Tests;

public sealed class ClientSessionServiceTests
{
    private static readonly DateTime ProcessStartedAt = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
    private const int ProcessId = 4321;
    private const string ExecutablePath = @"C:\Roblox\Versions\version-test\RobloxPlayerBeta.exe";

    [Fact]
    public async Task LaunchAsync_PersistsAccountAssociationAndUnverifiedAuthentication()
    {
        var (service, repository, launcher, inspector) = CreateService();
        inspector.Inspection = RunningInspection();

        var session = await service.LaunchAsync(AccountId);

        session.Status.Should().Be(ClientSessionStatus.Running);
        session.AccountUsername.Should().Be("ProfileOne");
        session.ProcessId.Should().Be(ProcessId);
        session.ExecutablePath.Should().Be(ExecutablePath);
        session.AuthenticationStatus.Should().Contain("Unverified");
        repository.Sessions.Should().ContainSingle();
        launcher.Verify(value => value.LaunchAsync(It.IsAny<RobloxInstallation>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchAsync_RejectsSecondManagedSessionWithoutLaunchingAgain()
    {
        var existingSession = ActiveSession();
        var (service, _, launcher, inspector) = CreateService(existingSession);
        inspector.Inspection = RunningInspection();

        var act = () => service.LaunchAsync(AccountId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already starting or running*");
        launcher.Verify(value => value.LaunchAsync(It.IsAny<RobloxInstallation>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LaunchAsync_ConcurrentRequestsOnlyStartOneManagedProcess()
    {
        var (service, repository, launcher, _) = CreateService();
        var firstLaunch = service.LaunchAsync(AccountId);
        var secondLaunch = service.LaunchAsync(AccountId);

        var act = () => Task.WhenAll(firstLaunch, secondLaunch);

        await act.Should().ThrowAsync<InvalidOperationException>();
        repository.Sessions.Should().ContainSingle();
        launcher.Verify(value => value.LaunchAsync(It.IsAny<RobloxInstallation>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchAsync_RejectsUnmanagedPlayerWithoutAssociatingIt()
    {
        var (service, repository, launcher, inspector) = CreateService();
        inspector.UnmanagedPlayerRunning = true;

        var act = () => service.LaunchAsync(AccountId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*outside RAM Plus*");
        repository.Sessions.Should().BeEmpty();
        launcher.Verify(value => value.LaunchAsync(It.IsAny<RobloxInstallation>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_AfterApplicationRestartMarksExitedProcessAsExited()
    {
        var existingSession = ActiveSession();
        var (service, repository, _, inspector) = CreateService(existingSession);
        inspector.Inspection = new ProcessInspectionResult(ProcessInspectionStatus.Exited);

        var sessions = await service.RefreshAsync();

        sessions.Should().ContainSingle();
        sessions[0].Status.Should().Be(ClientSessionStatus.Exited);
        sessions[0].EndedAtUtc.Should().NotBeNull();
        repository.Sessions[0].Status.Should().Be(ClientSessionStatus.Exited);
    }

    [Fact]
    public async Task RefreshAsync_DoesNotAdoptReusedProcessId()
    {
        var existingSession = ActiveSession();
        var (service, repository, _, inspector) = CreateService(existingSession);
        inspector.Inspection = new ProcessInspectionResult(
            ProcessInspectionStatus.Running,
            new ClientProcessSnapshot(ProcessId, ProcessStartedAt.AddSeconds(5), ExecutablePath));

        var sessions = await service.RefreshAsync();

        sessions[0].Status.Should().Be(ClientSessionStatus.Exited);
        sessions[0].LastError.Should().Contain("no longer matches");
        repository.Sessions[0].Status.Should().Be(ClientSessionStatus.Exited);
    }

    [Fact]
    public async Task RefreshAsync_DoesNotAdoptRunningProcessWithoutRecordedStartTime()
    {
        var existingSession = ActiveSession();
        existingSession.ProcessStartedAtUtc = null;
        var (service, _, _, inspector) = CreateService(existingSession);
        inspector.Inspection = RunningInspection();

        var sessions = await service.RefreshAsync();

        sessions[0].Status.Should().Be(ClientSessionStatus.StatusUnavailable);
        sessions[0].IsActive.Should().BeTrue();
        sessions[0].LastError.Should().Contain("will not attach");
    }

    [Fact]
    public async Task CloseAsync_TerminatesOnlyTheVerifiedManagedIdentity()
    {
        var existingSession = ActiveSession();
        var (service, repository, _, inspector) = CreateService(existingSession);
        inspector.Inspection = RunningInspection();

        var closed = await service.CloseAsync(existingSession.Id);

        closed.Should().BeTrue();
        inspector.Terminated.Should().NotBeNull();
        inspector.Terminated!.ProcessId.Should().Be(ProcessId);
        inspector.Terminated.ExecutablePath.Should().Be(ExecutablePath);
        repository.Sessions[0].Status.Should().Be(ClientSessionStatus.Exited);
    }

    [Fact]
    public async Task LaunchAsync_RecordsFailureWithoutClaimingAuthentication()
    {
        var (service, repository, launcher, _) = CreateService();
        launcher.Setup(value => value.LaunchAsync(It.IsAny<RobloxInstallation>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LaunchResult.Failed("Signature verification failed."));

        var session = await service.LaunchAsync(AccountId);

        session.Status.Should().Be(ClientSessionStatus.LaunchFailed);
        session.LastError.Should().Contain("Signature verification failed");
        session.AuthenticationStatus.Should().Contain("Unverified");
        repository.Sessions.Should().ContainSingle();
    }

    [Fact]
    public async Task LaunchAsync_WithoutCapturedStartTimeDoesNotAssociateProcess()
    {
        var (service, repository, launcher, inspector) = CreateService();
        launcher.Setup(value => value.LaunchAsync(It.IsAny<RobloxInstallation>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LaunchResult.Succeeded(ProcessId));
        inspector.Inspection = RunningInspection();

        var session = await service.LaunchAsync(AccountId);

        session.Status.Should().Be(ClientSessionStatus.StatusUnavailable);
        session.ProcessStartedAtUtc.Should().BeNull();
        session.AuthenticationStatus.Should().Contain("Unverified");
        session.LastError.Should().Contain("will not be associated");
        repository.Sessions.Should().ContainSingle();
    }

    private static readonly Guid AccountId = Guid.NewGuid();

    private static (ClientSessionService Service, FakeSessionRepository Repository, Mock<IRobloxLauncher> Launcher, FakeProcessInspector Inspector)
        CreateService(params ClientSession[] sessions)
    {
        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(repository => repository.GetByIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { Id = AccountId, Username = "ProfileOne", DisplayName = "One" });

        var repository = new FakeSessionRepository(sessions);
        var launcher = new Mock<IRobloxLauncher>();
        launcher.Setup(value => value.FindPreferredInstallation())
            .Returns(new RobloxInstallation { ExecutablePath = ExecutablePath, IsSignatureVerified = true });
        launcher.Setup(value => value.LaunchAsync(
                It.IsAny<RobloxInstallation>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LaunchResult.Succeeded(ProcessId, ProcessStartedAt));

        var inspector = new FakeProcessInspector();
        var service = new ClientSessionService(accounts.Object, repository, inspector, launcher.Object);
        return (service, repository, launcher, inspector);
    }

    private static ClientSession ActiveSession() => new()
    {
        AccountId = AccountId,
        AccountUsername = "ProfileOne",
        ProcessId = ProcessId,
        ProcessStartedAtUtc = ProcessStartedAt,
        ExecutablePath = ExecutablePath,
        Status = ClientSessionStatus.Running
    };

    private static ProcessInspectionResult RunningInspection() => new(
        ProcessInspectionStatus.Running,
        new ClientProcessSnapshot(ProcessId, ProcessStartedAt, ExecutablePath));

    private sealed class FakeSessionRepository : IClientSessionRepository
    {
        public List<ClientSession> Sessions { get; }

        public FakeSessionRepository(IEnumerable<ClientSession> sessions)
        {
            Sessions = sessions.ToList();
        }

        public Task<IReadOnlyList<ClientSession>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ClientSession>>(Sessions.ToArray());

        public Task AddAsync(ClientSession session, CancellationToken cancellationToken = default)
        {
            if (Sessions.Any(existing => existing.IsActive) && session.IsActive)
            {
                throw new ClientSessionConflictException();
            }

            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ClientSession session, CancellationToken cancellationToken = default)
        {
            var index = Sessions.FindIndex(existing => existing.Id == session.Id);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }

            Sessions[index] = session;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProcessInspector : IClientProcessInspector
    {
        public ProcessInspectionResult Inspection { get; set; } = RunningInspection();
        public bool UnmanagedPlayerRunning { get; set; }
        public int RunningPlayerCount { get; set; }
        public ClientProcessSnapshot? Terminated { get; private set; }

        public ProcessInspectionResult Inspect(int processId) => Inspection;

        public int CountRunningRobloxPlayers() => RunningPlayerCount;

        public bool HasUnmanagedRobloxPlayer(IReadOnlyCollection<int> managedProcessIds) => UnmanagedPlayerRunning;

        public bool Terminate(ClientProcessSnapshot expectedProcess)
        {
            Terminated = expectedProcess;
            Inspection = new ProcessInspectionResult(ProcessInspectionStatus.Exited);
            return true;
        }
    }
}