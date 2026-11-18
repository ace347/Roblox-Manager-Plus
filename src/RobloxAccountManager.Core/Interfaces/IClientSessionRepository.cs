using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Core.Interfaces;

public interface IClientSessionRepository
{
    Task<IReadOnlyList<ClientSession>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ClientSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(ClientSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ClearInactiveAsync(CancellationToken cancellationToken = default);
}