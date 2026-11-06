using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Security;

namespace RobloxAccountManager.Infrastructure.Security;

public sealed class CredentialVault : ICredentialVault
{
    private readonly IAccountRepository _repository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<CredentialVault> _logger;

    public CredentialVault(
        IAccountRepository repository,
        IEncryptionService encryptionService,
        ILogger<CredentialVault> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task StoreSecretAsync(Guid accountId, SensitiveBuffer secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(secret);

        var account = await _repository.GetByIdAsync(accountId, cancellationToken);
        if (account == null)
        {
            throw new KeyNotFoundException($"Account '{accountId}' not found in vault.");
        }

        // Generate per-record cryptographically strong salt / entropy
        var entropy = new byte[32];
        RandomNumberGenerator.Fill(entropy);

        byte[] ciphertext;
        var rawPlaintext = secret.DangerousGetBuffer();
        try
        {
            ciphertext = _encryptionService.Encrypt(rawPlaintext, entropy);
        }
        finally
        {
            // Do not keep references to raw plaintext
        }

        account.EncryptedSecret = ciphertext;
        account.SecretEntropy = entropy;
        await _repository.UpdateAsync(account, cancellationToken);

        _logger.LogInformation("Encrypted credentials securely stored for account {AccountId}", accountId);
    }

    public async Task<SensitiveBuffer?> RetrieveSecretAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.EncryptedSecret.Length == 0)
        {
            return null;
        }

        byte[] decrypted;
        try
        {
            decrypted = _encryptionService.Decrypt(account.EncryptedSecret, account.SecretEntropy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt credentials for account {AccountId}", accountId);
            throw;
        }

        var buffer = new SensitiveBuffer(decrypted);
        // Wipe decrypted transient array immediately after loading into SensitiveBuffer
        CryptographicOperations.ZeroMemory(decrypted);

        return buffer;
    }

    public async Task ClearSecretAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(accountId, cancellationToken);
        if (account != null)
        {
            account.EncryptedSecret = Array.Empty<byte>();
            account.SecretEntropy = null;
            await _repository.UpdateAsync(account, cancellationToken);
            _logger.LogInformation("Wiped stored encrypted secret for account {AccountId}", accountId);
        }
    }
}
