using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Core.Interfaces;

/// <summary>
/// Provides cryptographic signature verification for Windows binaries using Authenticode and WinVerifyTrust.
/// </summary>
public interface IAuthenticodeVerifier
{
    /// <summary>
    /// Verifies the Authenticode signature of the specified binary.
    /// Evaluates signature integrity, trust chain, revocation status, and publisher identity.
    /// </summary>
    /// <param name="filePath">Absolute path to the executable or library.</param>
    /// <param name="expectedPublishers">
    /// Optional collection of exact publisher Subject Common Names (e.g., "Roblox Corporation")
    /// that the signer certificate must match. If null or empty, any valid trusted publisher is accepted.
    /// </param>
    /// <returns>An <see cref="AuthenticodeResult"/> detailing verification outcome.</returns>
    AuthenticodeResult VerifyFile(string filePath, IEnumerable<string>? expectedPublishers = null);
}

