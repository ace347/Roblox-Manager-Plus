using System.Security.Cryptography;
using RobloxAccountManager.Core.Interfaces;

namespace RobloxAccountManager.Infrastructure.Security;

/// <summary>
/// Encrypts and decrypts sensitive buffers using Windows Data Protection API (DPAPI).
/// Bound exclusively to DataProtectionScope.CurrentUser.
/// </summary>
public sealed class DpapiEncryptionService : IEncryptionService
{
    public byte[] Encrypt(byte[] plaintext, byte[]? optionalEntropy = null)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        if (plaintext.Length == 0)
        {
            return Array.Empty<byte>();
        }

        try
        {
            return ProtectedData.Protect(plaintext, optionalEntropy, DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Failed to encrypt sensitive data via Windows DPAPI.", ex);
        }
    }

    public byte[] Decrypt(byte[] ciphertext, byte[]? optionalEntropy = null)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        if (ciphertext.Length == 0)
        {
            return Array.Empty<byte>();
        }

        try
        {
            return ProtectedData.Unprotect(ciphertext, optionalEntropy, DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Failed to decrypt sensitive data via Windows DPAPI. Ensure current Windows user matches encryption author.", ex);
        }
    }
}
