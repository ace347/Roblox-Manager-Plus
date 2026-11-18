namespace RobloxAccountManager.Core.Interfaces;

/// <summary>
/// Manages system-level synchronization primitives to allow multiple Roblox client instances
/// to run concurrently on the same machine.
/// </summary>
public interface IRobloxMultiInstanceManager : IDisposable
{
    /// <summary>
    /// Gets whether multi-instance mode is currently active.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables multi-instance mode by claiming singleton synchronization primitives.
    /// </summary>
    /// <returns>True if successfully claimed; otherwise false.</returns>
    bool Enable();

    /// <summary>
    /// Disables multi-instance mode and releases held synchronization primitives.
    /// </summary>
    void Disable();
}
