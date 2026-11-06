using Microsoft.Extensions.Logging;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Application.Services;

public interface IAccountService
{
    Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> SearchAccountsAsync(string searchTerm, string? tag = null, CancellationToken cancellationToken = default);
    Task CreateAccountAsync(string username, string? displayName, string? tag, string? notes, bool isDefault, CancellationToken cancellationToken = default);
    Task UpdateAccountMetadataAsync(Guid id, string username, string? displayName, string? tag, string? notes, bool isDefault, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetDefaultAccountAsync(Guid id, CancellationToken cancellationToken = default);
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

    public async Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> SearchAccountsAsync(string searchTerm, string? tag = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) && string.IsNullOrWhiteSpace(tag))
        {
            return await _repository.GetAllAsync(cancellationToken);
        }

        return await _repository.SearchAsync(searchTerm.Trim(), tag, cancellationToken);
    }

    public async Task CreateAccountAsync(string username, string? displayName, string? tag, string? notes, bool isDefault, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty.", nameof(username));
        }

        var existing = await _repository.GetByUsernameAsync(username.Trim(), cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"An account with username '{username}' already exists.");
        }

        var account = new Account
        {
            Username = username.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim(),
            Tag = string.IsNullOrWhiteSpace(tag) ? "Main" : tag.Trim(),
            Notes = notes?.Trim(),
            IsDefault = isDefault,
            EncryptedSecret = Array.Empty<byte>() // Credentials will be handled via DPAPI in Phase 4
        };

        await _repository.AddAsync(account, cancellationToken);
    }

    public async Task UpdateAccountMetadataAsync(Guid id, string username, string? displayName, string? tag, string? notes, bool isDefault, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            throw new KeyNotFoundException($"Account with id '{id}' not found.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty.", nameof(username));
        }

        // If username changed, check for conflict
        if (!string.Equals(account.Username, username.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await _repository.GetByUsernameAsync(username.Trim(), cancellationToken);
            if (conflict != null && conflict.Id != id)
            {
                throw new InvalidOperationException($"Username '{username}' is already in use by another account.");
            }
        }

        account.Username = username.Trim();
        account.DisplayName = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim();
        account.Tag = string.IsNullOrWhiteSpace(tag) ? "Main" : tag.Trim();
        account.Notes = notes?.Trim();
        account.IsDefault = isDefault;

        await _repository.UpdateAsync(account, cancellationToken);
    }

    public async Task DeleteAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
    }

    public async Task SetDefaultAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.SetDefaultAsync(id, cancellationToken);
    }
}
