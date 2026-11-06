namespace RobloxAccountManager.Core.Models;

/// <summary>
/// Result returned by the Roblox process launcher after attempting to start the client.
/// </summary>
public sealed class LaunchResult
{
    /// <summary>Whether the process was started successfully.</summary>
    public bool Success { get; init; }

    /// <summary>The OS process ID of the launched client, or null if launch failed.</summary>
    public int? ProcessId { get; init; }

    /// <summary>Human-readable description of a failure reason, or null on success.</summary>
    public string? FailureReason { get; init; }

    /// <summary>Creates a successful launch result.</summary>
    public static LaunchResult Succeeded(int pid) => new() { Success = true, ProcessId = pid };

    /// <summary>Creates a failed launch result.</summary>
    public static LaunchResult Failed(string reason) => new() { Success = false, FailureReason = reason };
}

