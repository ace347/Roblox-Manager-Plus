using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.App.Models;
using RobloxAccountManager.Application.Services;

namespace RobloxAccountManager.App.ViewModels;

public partial class AccountsViewModel : ObservableObject
{
    private readonly IAccountService _accountService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedTag = "All";

    [ObservableProperty]
    private AccountDisplayModel? _selectedAccount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private string? _errorMessage;

    // Form inputs for Add / Edit Dialog
    [ObservableProperty]
    private bool _isDialogOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private Guid? _editingAccountId;

    [ObservableProperty]
    private string _formUsername = string.Empty;

    [ObservableProperty]
    private string _formDisplayName = string.Empty;

    [ObservableProperty]
    private string _formTag = "Main";

    [ObservableProperty]
    private string _formNotes = string.Empty;

    [ObservableProperty]
    private bool _formIsDefault;

    public ObservableCollection<AccountDisplayModel> Accounts { get; } = new();

    public ObservableCollection<string> AvailableTags { get; } = new()
    {
        "All", "Main", "Alt", "Developer", "Trading"
    };

    public AccountsViewModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task InitializeAsync()
    {
        await LoadAccountsAsync();
    }

    [RelayCommand]
    public async Task LoadAccountsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            Accounts.Clear();

            var tagFilter = SelectedTag == "All" ? null : SelectedTag;
            var accounts = await _accountService.SearchAccountsAsync(SearchQuery, tagFilter);

            foreach (var acc in accounts)
            {
                Accounts.Add(new AccountDisplayModel
                {
                    Id = acc.Id,
                    Username = acc.Username,
                    DisplayName = string.IsNullOrWhiteSpace(acc.DisplayName) ? acc.Username : acc.DisplayName,
                    Tag = acc.Tag ?? "Main",
                    Notes = acc.Notes,
                    IsDefault = acc.IsDefault,
                    LastActive = acc.LastUsedAtUtc.HasValue ? acc.LastUsedAtUtc.Value.ToLocalTime().ToString("g") : "Never"
                });
            }

            IsEmpty = Accounts.Count == 0;
            if (Accounts.Count > 0 && SelectedAccount == null)
            {
                SelectedAccount = Accounts[0];
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load accounts: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditMode = false;
        EditingAccountId = null;
        FormUsername = string.Empty;
        FormDisplayName = string.Empty;
        FormTag = "Main";
        FormNotes = string.Empty;
        FormIsDefault = false;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(AccountDisplayModel? account)
    {
        if (account == null) return;

        IsEditMode = true;
        EditingAccountId = account.Id;
        FormUsername = account.Username;
        FormDisplayName = account.DisplayName;
        FormTag = account.Tag;
        FormNotes = account.Notes ?? string.Empty;
        FormIsDefault = account.IsDefault;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        IsDialogOpen = false;
    }

    [RelayCommand]
    private async Task SaveAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(FormUsername))
        {
            ErrorMessage = "Username is required.";
            return;
        }

        try
        {
            if (IsEditMode && EditingAccountId.HasValue)
            {
                await _accountService.UpdateAccountMetadataAsync(
                    EditingAccountId.Value,
                    FormUsername,
                    FormDisplayName,
                    FormTag,
                    FormNotes,
                    FormIsDefault
                );
            }
            else
            {
                await _accountService.CreateAccountAsync(
                    FormUsername,
                    FormDisplayName,
                    FormTag,
                    FormNotes,
                    FormIsDefault
                );
            }

            IsDialogOpen = false;
            await LoadAccountsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(AccountDisplayModel? account)
    {
        if (account == null) return;

        try
        {
            await _accountService.DeleteAccountAsync(account.Id);
            await LoadAccountsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete account: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SetAsDefaultAsync(AccountDisplayModel? account)
    {
        if (account == null) return;

        try
        {
            await _accountService.SetDefaultAccountAsync(account.Id);
            await LoadAccountsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to set default account: {ex.Message}";
        }
    }
}
