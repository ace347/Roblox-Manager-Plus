using RobloxAccountManager.Core.Security;

namespace RobloxAccountManager.Core.Interfaces;

public interface IRobloxAuthTicketService
{
    Task<string> GetAuthTicketAsync(SensitiveBuffer cookieBuffer, CancellationToken cancellationToken = default);
}
