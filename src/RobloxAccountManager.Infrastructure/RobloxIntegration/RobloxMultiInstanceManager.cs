using System.Threading;
using Microsoft.Extensions.Logging;
using RobloxAccountManager.Core.Interfaces;

namespace RobloxAccountManager.Infrastructure.RobloxIntegration;

/// <summary>
/// Pre-emptively creates and holds the Roblox singleton synchronization primitives
/// (<c>ROBLOX_singletonMutex</c> and <c>ROBLOX_singletonEvent</c>).
///
/// When RobloxPlayerBeta.exe starts, it calls the Win32 API CreateEvent("ROBLOX_singletonEvent").
/// Because this manager holds that name as a Mutex object, the Win32 call returns a type mismatch
/// error (STATUS_OBJECT_TYPE_MISMATCH / ERROR_INVALID_HANDLE).
///
/// As a result, the client's internal check:
///   <c>if (hEvent != NULL &amp;&amp; GetLastError() == ERROR_ALREADY_EXISTS)</c>
/// evaluates to false, allowing multiple Roblox game windows to launch and run concurrently.
/// </summary>
public sealed class RobloxMultiInstanceManager : IRobloxMultiInstanceManager
{
    private const string SingletonMutexName = "ROBLOX_singletonMutex";
    private const string SingletonEventName = "ROBLOX_singletonEvent";

    private readonly ILogger<RobloxMultiInstanceManager> _logger;
    private readonly object _lock = new();

    private Mutex? _singletonMutex;
    private Mutex? _singletonEvent;

    public RobloxMultiInstanceManager(ILogger<RobloxMultiInstanceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsEnabled { get; private set; }

    /// <inheritdoc/>
    public bool Enable()
    {
        lock (_lock)
        {
            if (IsEnabled)
            {
                return true;
            }

            try
            {
                var createdMutex = false;
                var createdEvent = false;

                if (_singletonMutex == null)
                {
                    _singletonMutex = new Mutex(true, SingletonMutexName, out createdMutex);
                }

                if (_singletonEvent == null)
                {
                    _singletonEvent = new Mutex(true, SingletonEventName, out createdEvent);
                }

                IsEnabled = true;
                _logger.LogInformation(
                    "Roblox multi-instance mode enabled (Mutex: {MutexCreated}, Event: {EventCreated}).",
                    createdMutex, createdEvent);
                return true;
            }
            catch (WaitHandleCannotBeOpenedException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not claim Roblox singleton synchronization handles. An existing Roblox instance may already hold it as an Event.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Roblox multi-instance synchronization objects.");
                return false;
            }
        }
    }

    /// <inheritdoc/>
    public void Disable()
    {
        lock (_lock)
        {
            try
            {
                _singletonMutex?.Dispose();
                _singletonMutex = null;

                _singletonEvent?.Dispose();
                _singletonEvent = null;

                IsEnabled = false;
                _logger.LogInformation("Roblox multi-instance mode disabled.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing multi-instance synchronization handles.");
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose() => Disable();
}
