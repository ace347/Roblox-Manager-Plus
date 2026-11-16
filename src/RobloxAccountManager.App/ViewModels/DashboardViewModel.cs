using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.App.Models;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly IClientSessionService _clientSessions;

    [ObservableProperty]
    private int _totalAccountsCount;

    [ObservableProperty]
    private int _storedSecretsCount;

    [ObservableProperty]
    private int? _activeSessionsCount;

    [ObservableProperty]
    private bool _isProcessCountAvailable;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _hasUnmanagedPlayerProcess;

    [ObservableProperty]
    private bool _isLaunching;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _launchMessage;

    [ObservableProperty]
    private AccountDisplayModel? _selectedAccount;

    public ObservableCollection<AccountDisplayModel> RecentAccounts { get; } = new();

    public DashboardViewModel(IAccountService accountService, IClientSessionService clientSessions)
    {
        _accountService = accountService;
        _clientSessions = clientSessions;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var accounts = await _accountService.GetAccountsAsync();
                TotalAccountsCount = accounts.Count;
                StoredSecretsCount = accounts.Count(account => account.EncryptedSecret.Length > 0);
                RecentAccounts.Clear();

                foreach (var acc in accounts.Take(5))
                {
                    RecentAccounts.Add(new AccountDisplayModel
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

                IsEmpty = accounts.Count == 0;
                SelectedAccount = RecentAccounts.FirstOrDefault(a => a.IsDefault) ?? RecentAccounts.FirstOrDefault();
            }
            catch (Exception)
            {
                TotalAccountsCount = 0;
                ActiveSessionsCount = null;
                IsProcessCountAvailable = false;
                StoredSecretsCount = 0;
                RecentAccounts.Clear();
                SelectedAccount = null;
                IsEmpty = false;
                ErrorMessage = "Unable to load dashboard data from the local database.";
            }

            if (ErrorMessage is null)
            {
                try
                {
                    ActiveSessionsCount = await _clientSessions.GetRunningPlayerProcessCountAsync();
                    HasUnmanagedPlayerProcess = await _clientSessions.HasUnmanagedPlayerProcessAsync();
                    IsProcessCountAvailable = true;
                }
                catch (Exception)
                {
                    ActiveSessionsCount = null;
                    HasUnmanagedPlayerProcess = false;
                    IsProcessCountAvailable = false;
                    ErrorMessage = "Unable to inspect local Roblox Player processes.";
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanQuickLaunch))]
    private Task QuickLaunchAsync() => LaunchAccountAsync(SelectedAccount);

    [RelayCommand(CanExecute = nameof(CanLaunchAccount))]
    private async Task LaunchAccountAsync(AccountDisplayModel? account)
    {
        if (account is null)
        {
            return;
        }

        IsLaunching = true;
        ErrorMessage = null;
        LaunchMessage = null;
        try
        {
            var session = await _clientSessions.LaunchAsync(account.Id);
            if (session.Status is ClientSessionStatus.Running or ClientSessionStatus.StatusUnavailable)
            {
                LaunchMessage = $"Roblox Player started for {session.AccountUsername}; sign-in must be checked in the official client.";
            }
            else
            {
                ErrorMessage = session.LastError ?? "Roblox Player could not be started.";
            }
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to start Roblox Player. Please try again.";
        }
        finally
        {
            IsLaunching = false;
            await RefreshAsync();
        }
    }

    private bool CanQuickLaunch() =>
        SelectedAccount is not null && IsProcessCountAvailable && ActiveSessionsCount == 0 && !HasUnmanagedPlayerProcess && !IsLaunching;

    private bool CanLaunchAccount(AccountDisplayModel? account) =>
        account is not null && IsProcessCountAvailable && ActiveSessionsCount == 0 && !HasUnmanagedPlayerProcess && !IsLaunching;

    partial void OnSelectedAccountChanged(AccountDisplayModel? value) =>
        QuickLaunchCommand.NotifyCanExecuteChanged();

    partial void OnActiveSessionsCountChanged(int? value)
    {
        QuickLaunchCommand.NotifyCanExecuteChanged();
        LaunchAccountCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsProcessCountAvailableChanged(bool value)
    {
        QuickLaunchCommand.NotifyCanExecuteChanged();
        LaunchAccountCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasUnmanagedPlayerProcessChanged(bool value)
    {
        QuickLaunchCommand.NotifyCanExecuteChanged();
        LaunchAccountCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLaunchingChanged(bool value)
    {
        QuickLaunchCommand.NotifyCanExecuteChanged();
        LaunchAccountCommand.NotifyCanExecuteChanged();
    }
}
