namespace RobloxAccountManager.Core.Interfaces;

public interface IEncryptionService
{
    byte[] Encrypt(byte[] plaintext, byte[]? optionalEntropy = null);
    byte[] Decrypt(byte[] ciphertext, byte[]? optionalEntropy = null);
}
