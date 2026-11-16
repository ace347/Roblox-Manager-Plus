using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.App.Models;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.App.ViewModels;

public partial class SessionsViewModel : ObservableObject
{
    private readonly IAccountService _accountsService;
    private readonly IClientSessionService _sessionsService;

    [ObservableProperty]
    private AccountDisplayModel? _selectedAccount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLaunching;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _hasUnmanagedPlayerProcess;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<AccountDisplayModel> Accounts { get; } = new();
    public ObservableCollection<ClientSession> Sessions { get; } = new();

    public SessionsViewModel(IAccountService accountsService, IClientSessionService sessionsService)
    {
        _accountsService = accountsService;
        _sessionsService = sessionsService;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            StatusMessage = null;

            var accounts = await _accountsService.GetAccountsAsync();
            Accounts.Clear();
            foreach (var account in accounts)
            {
                Accounts.Add(new AccountDisplayModel
                {
                    Id = account.Id,
                    Username = account.Username,
                    DisplayName = string.IsNullOrWhiteSpace(account.DisplayName) ? account.Username : account.DisplayName,
                    Tag = account.Tag ?? "Main",
                    Notes = account.Notes,
                    IsDefault = account.IsDefault,
                    LastActive = account.LastUsedAtUtc.HasValue ? account.LastUsedAtUtc.Value.ToLocalTime().ToString("g") : "Never"
                });
            }

            if (SelectedAccount is null || Accounts.All(account => account.Id != SelectedAccount.Id))
            {
                SelectedAccount = Accounts.FirstOrDefault(account => account.IsDefault) ?? Accounts.FirstOrDefault();
            }

            var sessions = await _sessionsService.RefreshAsync();
            Sessions.Clear();
            foreach (var session in sessions)
            {
                Sessions.Add(session);
            }

            HasUnmanagedPlayerProcess = await _sessionsService.HasUnmanagedPlayerProcessAsync();
            IsEmpty = Sessions.Count == 0;
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to refresh Roblox Player session information.";
            IsEmpty = Sessions.Count == 0;
        }
        finally
        {
            IsLoading = false;
            LaunchSelectedCommand.NotifyCanExecuteChanged();
            CloseSessionCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanLaunchSelected))]
    private async Task LaunchSelectedAsync()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        IsLaunching = true;
        ErrorMessage = null;
        StatusMessage = null;
        string? statusMessage = null;
        string? launchError = null;
        try
        {
            var session = await _sessionsService.LaunchAsync(SelectedAccount.Id);
            if (session.Status is ClientSessionStatus.Running or ClientSessionStatus.StatusUnavailable)
            {
                statusMessage = $"Player started for {session.AccountUsername}. Sign-in remains unverified; check the official Roblox client.";
            }
            else
            {
                launchError = session.LastError ?? "Roblox Player could not be started.";
            }
        }
        catch (InvalidOperationException ex)
        {
            launchError = ex.Message;
        }
        catch (Exception)
        {
            launchError = "Unable to start Roblox Player. Please try again.";
        }
        finally
        {
            IsLaunching = false;
        }

        await RefreshAsync();
        StatusMessage = statusMessage;
        ErrorMessage = launchError;
    }

    [RelayCommand(CanExecute = nameof(CanCloseSession))]
    private async Task CloseSessionAsync(ClientSession? session)
    {
        if (session is null)
        {
            return;
        }

        string? closeError = null;
        try
        {
            var closed = await _sessionsService.CloseAsync(session.Id);
            if (!closed)
            {
                closeError = "The process identity could not be verified, so it was not closed.";
            }
        }
        catch (Exception)
        {
            closeError = "Unable to close the managed Roblox Player process.";
        }

        await RefreshAsync();
        ErrorMessage = closeError;
    }

    private bool CanLaunchSelected() =>
        SelectedAccount is not null &&
        !IsLoading &&
        !IsLaunching &&
        !HasUnmanagedPlayerProcess &&
        Sessions.All(session => !session.IsActive);

    private static bool CanCloseSession(ClientSession? session) =>
        session?.Status == ClientSessionStatus.Running;

    partial void OnSelectedAccountChanged(AccountDisplayModel? value) =>
        LaunchSelectedCommand.NotifyCanExecuteChanged();

    partial void OnIsLaunchingChanged(bool value) =>
        LaunchSelectedCommand.NotifyCanExecuteChanged();

    partial void OnHasUnmanagedPlayerProcessChanged(bool value) =>
        LaunchSelectedCommand.NotifyCanExecuteChanged();
}