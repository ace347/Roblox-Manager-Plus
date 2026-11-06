using System.Security.Cryptography;
using RobloxAccountManager.Core.Interfaces;

namespace RobloxAccountManager.Infrastructure.Security;

public sealed class DpapiEncryptionService : IEncryptionService
{
    public byte[] Encrypt(byte[] plaintext, byte[]? optionalEntropy = null)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        return ProtectedData.Protect(plaintext, optionalEntropy, DataProtectionScope.CurrentUser);
    }

    public byte[] Decrypt(byte[] ciphertext, byte[]? optionalEntropy = null)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        return ProtectedData.Unprotect(ciphertext, optionalEntropy, DataProtectionScope.CurrentUser);
    }
}
