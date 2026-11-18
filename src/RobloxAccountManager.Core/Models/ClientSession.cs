namespace RobloxAccountManager.Core.Models;

public enum ClientSessionStatus
{
    Launching,
    Running,
    StatusUnavailable,
    Exited,
    LaunchFailed
}

public enum ProcessInspectionStatus
{
    Running,
    Exited,
    Unavailable
}

public sealed record ClientProcessSnapshot(int ProcessId, DateTime StartedAtUtc, string ExecutablePath);

public sealed record ProcessInspectionResult(
    ProcessInspectionStatus Status,
    ClientProcessSnapshot? Snapshot = null,
    string? Error = null);

public sealed class ClientSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AccountId { get; init; }
    public string AccountUsername { get; init; } = string.Empty;
    public string? AccountDisplayName { get; init; }
    public int? ProcessId { get; set; }
    public DateTime LaunchedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessStartedAtUtc { get; set; }
    public string ExecutablePath { get; set; } = string.Empty;
    public ClientSessionStatus Status { get; set; } = ClientSessionStatus.Launching;
    public DateTime? EndedAtUtc { get; set; }
    public string? LastError { get; set; }

    public bool IsActive => Status is ClientSessionStatus.Launching
        or ClientSessionStatus.Running
        or ClientSessionStatus.StatusUnavailable;

    public string AuthenticationStatus { get; set; } = "Unverified; check sign-in in Roblox Player";
}