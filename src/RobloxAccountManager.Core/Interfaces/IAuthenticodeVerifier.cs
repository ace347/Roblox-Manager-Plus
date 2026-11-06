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
    /// <param name="expectedPublisherSubstrings">
    /// Optional publisher substrings that must be present in the signer subject (e.g., "Roblox Corporation").
    /// If null or empty, only signature and trust validity are evaluated.
    /// </param>
    /// <returns>An <see cref="AuthenticodeResult"/> detailing verification outcome.</returns>
    AuthenticodeResult VerifyFile(string filePath, IEnumerable<string>? expectedPublisherSubstrings = null);
}
