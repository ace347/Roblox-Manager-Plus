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
    private string _formCookie = string.Empty;

    [ObservableProperty]
    private bool _formIsDefault;

    private readonly IClientSessionService? _sessionService;

    /// <summary>
    /// Raised when the ViewModel needs the View to open the Roblox login WebView window.
    /// The View opens the window, then invokes this delegate with the captured cookie + username + displayName,
    /// or returns null if the user cancelled.
    /// </summary>
    public event Func<Task<(string Cookie, string Username, string DisplayName)?>>? RequestRobloxLogin;

    public ObservableCollection<AccountDisplayModel> Accounts { get; } = new();

    public ObservableCollection<string> AvailableTags { get; } = new()
    {
        "All", "Main", "Alt", "Developer", "Trading"
    };

    public AccountsViewModel(IAccountService accountService, IClientSessionService? sessionService = null)
    {
        _accountService = accountService;
        _sessionService = sessionService;
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
            IsEmpty = false;
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

            await RefreshRunningSessionsAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to load accounts from the local database.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshRunningSessionsAsync()
    {
        if (_sessionService == null) return;

        try
        {
            var sessions = await _sessionService.RefreshAsync();
            var activeByAccountId = sessions
                .Where(s => s.IsActive)
                .ToDictionary(s => s.AccountId, s => s);

            foreach (var account in Accounts)
            {
                if (activeByAccountId.TryGetValue(account.Id, out var session))
                {
                    account.IsRunning = true;
                    account.SessionId = session.Id;
                    account.RunningProcessId = session.ProcessId;
                    account.AuthenticationStatus = session.ProcessId.HasValue
                        ? $"● Running (PID: {session.ProcessId})"
                        : "● Running";
                }
                else
                {
                    account.IsRunning = false;
                    account.SessionId = null;
                    account.RunningProcessId = null;
                    account.AuthenticationStatus = "Sign-in status not verified";
                }
            }
        }
        catch
        {
            // Transient background refresh failure, ignore
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
        FormCookie = string.Empty;
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
        FormCookie = string.Empty;
        FormIsDefault = account.IsDefault;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        IsDialogOpen = false;
    }

    [RelayCommand]
    private async Task LaunchAccountAsync(AccountDisplayModel? account)
    {
        if (account == null || _sessionService == null) return;

        try
        {
            ErrorMessage = null;
            await _sessionService.LaunchAsync(account.Id);
            await RefreshRunningSessionsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to launch {account.Username}: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CloseAccountSessionAsync(AccountDisplayModel? account)
    {
        if (account == null || _sessionService == null || !account.SessionId.HasValue) return;

        try
        {
            ErrorMessage = null;
            await _sessionService.CloseAsync(account.SessionId.Value);
            await RefreshRunningSessionsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to close session for {account.Username}: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoginWithRobloxAsync()
    {
        if (RequestRobloxLogin is null) return;

        var result = await RequestRobloxLogin.Invoke();
        if (result is null) return; // user cancelled

        var (cookie, username, displayName) = result.Value;

        if (string.IsNullOrWhiteSpace(username) || username == "{}")
        {
            ErrorMessage = "Could not identify Roblox username from login session.";
            return;
        }

        try
        {
            ErrorMessage = null;
            await _accountService.CreateAccountAsync(
                username,
                displayName: string.IsNullOrWhiteSpace(displayName) ? username : displayName,
                tag: "Main",
                notes: string.Empty,
                isDefault: false,
                secretCookie: cookie
            );
            await LoadAccountsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save account: {ex.Message}";
        }
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
                    FormIsDefault,
                    string.IsNullOrWhiteSpace(FormCookie) ? null : FormCookie
                );
            }
            else
            {
                await _accountService.CreateAccountAsync(
                    FormUsername,
                    FormDisplayName,
                    FormTag,
                    FormNotes,
                    FormIsDefault,
                    string.IsNullOrWhiteSpace(FormCookie) ? null : FormCookie
                );
            }

            IsDialogOpen = false;
            await LoadAccountsAsync();
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to save this account. Please try again.";
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
        catch (Exception)
        {
            ErrorMessage = "Unable to delete this account. Please try again.";
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
        catch (Exception)
        {
            ErrorMessage = "Unable to update the default account. Please try again.";
        }
    }
}
