using Microsoft.Extensions.Logging;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Infrastructure.RobloxIntegration;

/// <summary>
/// Discovers Roblox client installations from known local installation paths.
/// Uses <see cref="IAuthenticodeVerifier"/> to cryptographically verify executable integrity,
/// trust chain, and publisher authenticity via Windows WinVerifyTrust.
/// Only reads file-system metadata — never accesses the network, browser cookies, or Roblox APIs.
/// </summary>
public sealed class RobloxClientLocator
{
    // Known official Roblox publisher Common Names (exact Subject Common Name match).
    private static readonly string[] ExpectedPublishers = ["Roblox Corporation"];

    // Roblox executable filename.
    private const string PlayerExecutableName = "RobloxPlayerBeta.exe";

    // Well-known Roblox installation base directories.
    // The player is located at: <BaseDir>\Versions\version-<hash>\RobloxPlayerBeta.exe
    private static readonly IReadOnlyList<string> KnownBasePaths =
    [
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Roblox", "Versions"),
        Path.Combine(
            // %ProgramFiles(x86)%
            Environment.GetEnvironmentVariable("ProgramFiles(x86)")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), ".."),
            "Roblox", "Versions"),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Roblox", "Versions"),
    ];

    private readonly IAuthenticodeVerifier _authenticodeVerifier;
    private readonly ILogger<RobloxClientLocator> _logger;

    /// <summary>Initialises a new instance of <see cref="RobloxClientLocator"/>.</summary>
    public RobloxClientLocator(IAuthenticodeVerifier authenticodeVerifier, ILogger<RobloxClientLocator> logger)
    {
        _authenticodeVerifier = authenticodeVerifier ?? throw new ArgumentNullException(nameof(authenticodeVerifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Enumerates all valid Roblox installations found under the known base paths.
    /// Results are ordered by <see cref="RobloxInstallation.LastWriteUtc"/> descending (newest first).
    /// </summary>
    public IReadOnlyList<RobloxInstallation> DiscoverInstallations()
    {
        var results = new List<RobloxInstallation>();

        foreach (var baseDir in KnownBasePaths)
        {
            if (!Directory.Exists(baseDir))
            {
                _logger.LogDebug("Roblox search path does not exist, skipping: {Path}", baseDir);
                continue;
            }

            _logger.LogDebug("Scanning Roblox installations under: {Path}", baseDir);

            IEnumerable<string> versionDirs;
            try
            {
                versionDirs = Directory.EnumerateDirectories(baseDir, "version-*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied scanning Roblox path: {Path}", baseDir);
                continue;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "IO error scanning Roblox path: {Path}", baseDir);
                continue;
            }

            foreach (var versionDir in versionDirs)
            {
                var exePath = Path.Combine(versionDir, PlayerExecutableName);
                if (!File.Exists(exePath))
                    continue;

                var installation = BuildInstallation(exePath, versionDir);
                if (installation is not null)
                    results.Add(installation);
            }
        }

        // Deduplicate by resolved path and order newest first.
        return results
            .DistinctBy(i => Path.GetFullPath(i.ExecutablePath), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(i => i.LastWriteUtc)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Returns the preferred (newest, valid) installation, or <c>null</c> if none is found.
    /// </summary>
    public RobloxInstallation? FindPreferredInstallation()
    {
        var installations = DiscoverInstallations();
        if (installations.Count == 0)
        {
            _logger.LogInformation("No Roblox installations found on this machine.");
            return null;
        }

        var preferred = installations[0];
        _logger.LogInformation(
            "Preferred Roblox installation: {Path} (version: {Version}, signature verified: {Sig})",
            preferred.ExecutablePath, preferred.Version, preferred.IsSignatureVerified);
        return preferred;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private RobloxInstallation? BuildInstallation(string exePath, string versionDir)
    {
        FileInfo fileInfo;
        try
        {
            fileInfo = new FileInfo(exePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not stat executable: {Path}", exePath);
            return null;
        }

        // Basic sanity check: refuse zero-byte or impossibly small files before parsing.
        // NOTE: This size check is strictly an initial sanity filter, NOT a security verification.
        if (fileInfo.Length < 65_536)
        {
            _logger.LogWarning(
                "Executable at {Path} is suspiciously small ({Bytes} bytes) — skipping.",
                exePath, fileInfo.Length);
            return null;
        }

        var version = Path.GetFileName(versionDir); // e.g. "version-abc123"

        // Cryptographic Authenticode & WinVerifyTrust evaluation
        var verifyResult = _authenticodeVerifier.VerifyFile(exePath, ExpectedPublishers);
        var signatureVerified = verifyResult.IsValid;

        if (!signatureVerified)
        {
            _logger.LogWarning(
                "Authenticode verification failed for {Path}: {Status} ({Message}, HRESULT: 0x{HResult:X8})",
                exePath, verifyResult.Status, verifyResult.Message, verifyResult.RawHResult);
        }

        return new RobloxInstallation
        {
            ExecutablePath = exePath,
            Version = version,
            IsSignatureVerified = signatureVerified,
            LastWriteUtc = fileInfo.LastWriteTimeUtc,
        };
    }
}
