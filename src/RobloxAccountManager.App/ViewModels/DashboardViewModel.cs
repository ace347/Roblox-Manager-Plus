using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.App.Models;
using RobloxAccountManager.Application.Services;

namespace RobloxAccountManager.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IAccountService _accountService;

    [ObservableProperty]
    private int _totalAccountsCount;

    [ObservableProperty]
    private int _activeSessionsCount;

    [ObservableProperty]
    private string _securityStatus = "DPAPI Encrypted (Protected)";

    [ObservableProperty]
    private AccountDisplayModel? _selectedAccount;

    public ObservableCollection<AccountDisplayModel> RecentAccounts { get; } = new();

    public DashboardViewModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task RefreshAsync()
    {
        var accounts = await _accountService.GetAccountsAsync();
        TotalAccountsCount = accounts.Count;
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

        SelectedAccount = RecentAccounts.FirstOrDefault(a => a.IsDefault) ?? RecentAccounts.FirstOrDefault();
    }

    [RelayCommand]
    private void QuickLaunch()
    {
        if (SelectedAccount != null)
        {
            SecurityStatus = $"Launching {SelectedAccount.Username}...";
        }
    }
}
