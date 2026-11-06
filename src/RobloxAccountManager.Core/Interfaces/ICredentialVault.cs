using RobloxAccountManager.Core.Security;

namespace RobloxAccountManager.Core.Interfaces;

public interface ICredentialVault
{
    Task StoreSecretAsync(Guid accountId, SensitiveBuffer secret, CancellationToken cancellationToken = default);
    Task<SensitiveBuffer?> RetrieveSecretAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task ClearSecretAsync(Guid accountId, CancellationToken cancellationToken = default);
}
