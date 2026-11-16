using RobloxAccountManager.Core.Exceptions;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Application.Services;

public interface IClientSessionService
{
    Task<IReadOnlyList<ClientSession>> RefreshAsync(CancellationToken cancellationToken = default);
    Task<int> GetRunningPlayerProcessCountAsync(CancellationToken cancellationToken = default);
    Task<bool> HasUnmanagedPlayerProcessAsync(CancellationToken cancellationToken = default);
    Task<ClientSession> LaunchAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<bool> CloseAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public sealed class ClientSessionService : IClientSessionService, IDisposable
{
    private static readonly TimeSpan AbandonedLaunchTimeout = TimeSpan.FromSeconds(30);
    private readonly IAccountRepository _accounts;
    private readonly IClientSessionRepository _sessions;
    private readonly IClientProcessInspector _processes;
    private readonly IRobloxLauncher _launcher;
    private readonly SemaphoreSlim _operationGate = new(1, 1);

    public ClientSessionService(
        IAccountRepository accounts,
        IClientSessionRepository sessions,
        IClientProcessInspector processes,
        IRobloxLauncher launcher)
    {
        _accounts = accounts;
        _sessions = sessions;
        _processes = processes;
        _launcher = launcher;
    }

    public void Dispose() => _operationGate.Dispose();

    public async Task<IReadOnlyList<ClientSession>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            return await RefreshCoreAsync(cancellationToken);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public Task<int> GetRunningPlayerProcessCountAsync(CancellationToken cancellationToken = default) =>
        Task.Run(_processes.CountRunningRobloxPlayers, cancellationToken);

    public async Task<bool> HasUnmanagedPlayerProcessAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _sessions.GetAllAsync(cancellationToken);
        var managedProcessIds = sessions
            .Where(session => session.IsActive && session.ProcessId.HasValue)
            .Select(session => session.ProcessId!.Value)
            .ToArray();
        return await Task.Run(
            () => _processes.HasUnmanagedRobloxPlayer(managedProcessIds),
            cancellationToken);
    }

    public async Task<ClientSession> LaunchAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            var sessions = await RefreshCoreAsync(cancellationToken);
            if (sessions.Any(session => session.IsActive))
            {
                throw new InvalidOperationException(
                    "A RAM Plus managed Roblox Player process is already starting or running. Close it before launching another account; independent simultaneous sessions are not supported.");
            }

            if (await Task.Run(() => _processes.HasUnmanagedRobloxPlayer(Array.Empty<int>()), cancellationToken))
            {
                throw new InvalidOperationException(
                    "A Roblox Player process is already running outside RAM Plus. Its account and sign-in state cannot be verified; close it in Roblox before launching a managed session.");
            }

            var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
            if (account is null)
            {
                throw new KeyNotFoundException("The selected account profile no longer exists.");
            }

            var installation = _launcher.FindPreferredInstallation();
            var session = new ClientSession
            {
                AccountId = account.Id,
                AccountUsername = account.Username,
                AccountDisplayName = account.DisplayName,
                ExecutablePath = installation?.ExecutablePath ?? string.Empty,
                Status = ClientSessionStatus.Launching
            };

            try
            {
                await _sessions.AddAsync(session, cancellationToken);
            }
            catch (ClientSessionConflictException)
            {
                throw new InvalidOperationException(
                    "Another RAM Plus instance is already starting or tracking a Roblox Player process.");
            }

            if (installation is null)
            {
                await MarkFailedAsync(session, "No verified Roblox Player installation was found.", cancellationToken);
                return session;
            }

            try
            {
                var launch = await _launcher.LaunchAsync(installation, cancellationToken: cancellationToken);
                if (!launch.Success || launch.ProcessId is null)
                {
                    await MarkFailedAsync(session, launch.FailureReason ?? "The operating system did not start Roblox Player.", cancellationToken);
                    return session;
                }

                session.ProcessId = launch.ProcessId;
                var inspection = await Task.Run(
                    () => _processes.Inspect(launch.ProcessId.Value),
                    cancellationToken);

                if (inspection.Status == ProcessInspectionStatus.Running &&
                    inspection.Snapshot is not null &&
                    launch.ProcessStartedAtUtc.HasValue &&
                    inspection.Snapshot.StartedAtUtc == launch.ProcessStartedAtUtc.Value &&
                    IsExpectedClient(inspection.Snapshot, installation.ExecutablePath))
                {
                    session.ProcessStartedAtUtc = launch.ProcessStartedAtUtc.Value;
                    session.Status = ClientSessionStatus.Running;
                }
                else if (inspection.Status == ProcessInspectionStatus.Unavailable || !launch.ProcessStartedAtUtc.HasValue)
                {
                    session.Status = ClientSessionStatus.StatusUnavailable;
                    session.LastError = "The process started, but its identity could not be verified. It will not be associated with the saved profile.";
                }
                else
                {
                    session.Status = ClientSessionStatus.LaunchFailed;
                    session.EndedAtUtc = DateTime.UtcNow;
                    session.LastError = "The started process did not match the selected Roblox Player executable.";
                }

                await _sessions.UpdateAsync(session, cancellationToken);
                return session;
            }
            catch (OperationCanceledException)
            {
                await MarkFailedAsync(session, "The launch request was cancelled.", CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                await MarkFailedAsync(session, $"Launch failed ({ex.GetType().Name}).", CancellationToken.None);
                throw;
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<bool> CloseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            var sessions = await RefreshCoreAsync(cancellationToken);
            var session = sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is null ||
                session.Status != ClientSessionStatus.Running ||
                !session.ProcessId.HasValue ||
                !session.ProcessStartedAtUtc.HasValue)
            {
                return false;
            }

            var expected = new ClientProcessSnapshot(
                session.ProcessId.Value,
                session.ProcessStartedAtUtc.Value,
                session.ExecutablePath);
            var terminated = await Task.Run(
                () => _processes.Terminate(expected),
                cancellationToken);

            if (terminated)
            {
                session.Status = ClientSessionStatus.Exited;
                session.EndedAtUtc = DateTime.UtcNow;
                session.LastError = null;
            }
            else
            {
                session.Status = ClientSessionStatus.StatusUnavailable;
                session.LastError = "The managed process could not be closed or re-verified.";
            }

            await _sessions.UpdateAsync(session, cancellationToken);
            return terminated;
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task<IReadOnlyList<ClientSession>> RefreshCoreAsync(CancellationToken cancellationToken)
    {
        var sessions = await _sessions.GetAllAsync(cancellationToken);
        foreach (var session in sessions.Where(item => item.IsActive))
        {
            if (!session.ProcessId.HasValue)
            {
                if (session.Status == ClientSessionStatus.Launching &&
                    DateTime.UtcNow - session.LaunchedAtUtc < AbandonedLaunchTimeout)
                {
                    continue;
                }

                await MarkFailedAsync(session, "The launch ended before its process identity was recorded.", cancellationToken);
                continue;
            }

            var inspection = await Task.Run(
                () => _processes.Inspect(session.ProcessId.Value),
                cancellationToken);

            if (inspection.Status == ProcessInspectionStatus.Exited)
            {
                session.Status = ClientSessionStatus.Exited;
                session.EndedAtUtc = DateTime.UtcNow;
                session.LastError = null;
            }
            else if (inspection.Status == ProcessInspectionStatus.Unavailable)
            {
                session.Status = ClientSessionStatus.StatusUnavailable;
                session.LastError = inspection.Error ?? "The operating system could not verify the process.";
            }
            else if (inspection.Snapshot is null)
            {
                session.Status = ClientSessionStatus.Exited;
                session.EndedAtUtc = DateTime.UtcNow;
                session.LastError = "The process ID no longer matches the recorded client identity.";
            }
            else if (!session.ProcessStartedAtUtc.HasValue)
            {
                session.Status = ClientSessionStatus.StatusUnavailable;
                session.LastError = "The process is visible, but its original start time was not recorded, so RAM Plus will not attach to it.";
            }
            else if (!MatchesStoredProcess(session, inspection.Snapshot))
            {
                session.Status = ClientSessionStatus.Exited;
                session.EndedAtUtc = DateTime.UtcNow;
                session.LastError = "The process ID no longer matches the recorded client identity.";
            }
            else
            {
                session.Status = ClientSessionStatus.Running;
                session.LastError = null;
            }

            await _sessions.UpdateAsync(session, cancellationToken);
        }

        return sessions.OrderByDescending(item => item.LaunchedAtUtc).ToArray();
    }

    private async Task MarkFailedAsync(ClientSession session, string error, CancellationToken cancellationToken)
    {
        session.Status = ClientSessionStatus.LaunchFailed;
        session.EndedAtUtc = DateTime.UtcNow;
        session.LastError = error;
        await _sessions.UpdateAsync(session, cancellationToken);
    }

    private static bool MatchesStoredProcess(ClientSession session, ClientProcessSnapshot process) =>
        session.ProcessId == process.ProcessId &&
        session.ProcessStartedAtUtc.HasValue &&
        session.ProcessStartedAtUtc.Value == process.StartedAtUtc &&
        IsExpectedClient(process, session.ExecutablePath);

    private static bool IsExpectedClient(ClientProcessSnapshot process, string expectedPath) =>
        string.Equals(
            Path.GetFullPath(process.ExecutablePath),
            Path.GetFullPath(expectedPath),
            StringComparison.OrdinalIgnoreCase);
}