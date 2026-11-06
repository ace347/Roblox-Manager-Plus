using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Core.Interfaces;

/// <summary>
/// Discovers installed Roblox clients and launches the Roblox player process safely.
/// Implementations must never inject credentials into the process, extract browser cookies,
/// or use undocumented authentication parameters.
/// </summary>
public interface IRobloxLauncher
{
    /// <summary>
    /// Returns all Roblox installations found on this machine, ordered newest-first.
    /// Returns an empty collection if Roblox is not installed.
    /// </summary>
    IReadOnlyList<RobloxInstallation> DiscoverInstallations();

    /// <summary>
    /// Returns the preferred Roblox installation (newest valid executable), or null if none found.
    /// </summary>
    RobloxInstallation? FindPreferredInstallation();

    /// <summary>
    /// Launches the Roblox player using the specified installation and optional place URL.
    /// Only officially documented roblox:// protocol arguments are permitted.
    /// </summary>
    /// <param name="installation">The validated installation to launch.</param>
    /// <param name="placeUrl">
    /// Optional roblox:// protocol URL (e.g. "roblox://experiences/start?placeId=12345").
    /// Pass null to launch without navigating to a place.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<LaunchResult> LaunchAsync(
        RobloxInstallation installation,
        string? placeUrl = null,
        CancellationToken cancellationToken = default);
}
