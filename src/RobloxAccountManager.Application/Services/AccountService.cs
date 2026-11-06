using Microsoft.Extensions.Logging;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Application.Services;

public interface IAccountService
{
    Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken cancellationToken = default);
}

public sealed class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IAccountRepository repository, ILogger<AccountService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving accounts from repository.");
        return await _repository.GetAllAsync(cancellationToken);
    }
}
