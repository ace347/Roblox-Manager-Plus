namespace RobloxAccountManager.Core.Models;

/// <summary>
/// Represents a discovered Roblox client installation on the local machine.
/// Only the executable path and basic file metadata are stored here — no credentials or tokens.
/// </summary>
public sealed class RobloxInstallation
{
    /// <summary>Absolute path to the Roblox player executable.</summary>
    public string ExecutablePath { get; init; } = string.Empty;

    /// <summary>Roblox version string extracted from the parent directory name (e.g. "version-abc123").</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>Whether the executable's Authenticode signature was verified as published by Roblox Corporation.</summary>
    public bool IsSignatureVerified { get; init; }

    /// <summary>File last-write time in UTC, used to prefer the most recently installed version.</summary>
    public DateTime LastWriteUtc { get; init; }
}

