using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Infrastructure.RobloxIntegration;

/// <summary>
/// Implements <see cref="IRobloxLauncher"/> using <see cref="Process"/> to safely start the
/// Roblox player executable.
///
/// Security constraints enforced here:
/// - Only the official roblox:// URI scheme is accepted as an argument.
/// - Arguments are passed via <see cref="ProcessStartInfo.ArgumentList"/> (no shell interpolation).
/// - The executable path is validated before use.
/// - No credentials, session tokens, or authentication parameters are injected.
/// - UseShellExecute is false to prevent launching via the Windows shell.
/// </summary>
public sealed class RobloxProcessLauncher : IRobloxLauncher
{
    // Only the official roblox:// URI scheme is permitted as a launch argument.
    private const string AllowedUriScheme = "roblox";

    private readonly RobloxClientLocator _locator;
    private readonly ILogger<RobloxProcessLauncher> _logger;

    /// <summary>Initialises a new instance of <see cref="RobloxProcessLauncher"/>.</summary>
    public RobloxProcessLauncher(RobloxClientLocator locator, ILogger<RobloxProcessLauncher> logger)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public IReadOnlyList<RobloxInstallation> DiscoverInstallations() =>
        _locator.DiscoverInstallations();

    /// <inheritdoc/>
    public RobloxInstallation? FindPreferredInstallation() =>
        _locator.FindPreferredInstallation();

    /// <inheritdoc/>
    public async Task<LaunchResult> LaunchAsync(
        RobloxInstallation installation,
        string? placeUrl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installation);

        // 1. Validate the executable path.
        var validationError = ValidateExecutablePath(installation.ExecutablePath);
        if (validationError is not null)
        {
            _logger.LogError("Roblox executable validation failed: {Reason}", validationError);
            return LaunchResult.Failed($"Executable validation failed: {validationError}");
        }

        // 2. Validate the optional place URL.
        if (placeUrl is not null)
        {
            var urlError = ValidatePlaceUrl(placeUrl);
            if (urlError is not null)
            {
                _logger.LogError("Invalid place URL rejected: {Reason}", urlError);
                return LaunchResult.Failed($"Place URL rejected: {urlError}");
            }
        }

        // 3. Build ProcessStartInfo — no shell, explicit argument list.
        var psi = new ProcessStartInfo
        {
            FileName = installation.ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            RedirectStandardInput = false,
        };

        if (placeUrl is not null)
        {
            // Use ArgumentList (not Arguments string) to avoid shell injection.
            psi.ArgumentList.Add(placeUrl);
        }

        // 4. Launch.
        _logger.LogInformation(
            "Launching Roblox client: {Exe} | PlaceUrl: {Url}",
            installation.ExecutablePath,
            placeUrl ?? "(none)");

        try
        {
            // Offload to a thread pool thread to avoid blocking the UI thread on Process.Start().
            var process = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Process.Start(psi);
            }, cancellationToken).ConfigureAwait(false);

            if (process is null)
            {
                _logger.LogError("Process.Start returned null for {Exe}.", installation.ExecutablePath);
                return LaunchResult.Failed("Process.Start returned null — the OS did not create a process.");
            }

            _logger.LogInformation(
                "Roblox client launched successfully. PID: {Pid}", process.Id);

            return LaunchResult.Succeeded(process.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Roblox launch cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Roblox client at {Exe}.", installation.ExecutablePath);
            return LaunchResult.Failed($"Launch exception: {ex.GetType().Name} — {ex.Message}");
        }
    }

    // ── Validation helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Validates the executable path.
    /// Returns an error string on failure, or null on success.
    /// </summary>
    public static string? ValidateExecutablePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Executable path is null or empty.";

        // Must be an absolute path to prevent relative-path attacks.
        if (!Path.IsPathFullyQualified(path))
            return "Executable path must be absolute (fully qualified).";

        // Normalise and reject path traversal.
        var fullPath = Path.GetFullPath(path);
        if (!string.Equals(fullPath, path, StringComparison.OrdinalIgnoreCase))
        {
            // Allow normalisation differences (trailing slash, mixed separators).
            // But if the final path differs enough to suggest traversal, reject.
            if (fullPath.Contains("..", StringComparison.Ordinal))
                return "Executable path contains path traversal sequences.";
        }

        // Must be an .exe file.
        if (!string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
            return "Executable path must point to a .exe file.";

        // Must be named RobloxPlayerBeta.exe specifically.
        if (!string.Equals(Path.GetFileName(path), "RobloxPlayerBeta.exe", StringComparison.OrdinalIgnoreCase))
            return "Executable must be named RobloxPlayerBeta.exe.";

        // File must exist.
        if (!File.Exists(fullPath))
            return $"Executable not found: {fullPath}";

        return null; // OK
    }

    /// <summary>
    /// Validates that the place URL uses only the roblox:// scheme.
    /// Returns an error string on failure, or null on success.
    /// </summary>
    public static string? ValidatePlaceUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "Place URL is null or empty.";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "Place URL is not a valid absolute URI.";

        if (!string.Equals(uri.Scheme, AllowedUriScheme, StringComparison.OrdinalIgnoreCase))
            return $"Place URL scheme '{uri.Scheme}' is not permitted. Only '{AllowedUriScheme}://' is allowed.";

        return null; // OK
    }
}
