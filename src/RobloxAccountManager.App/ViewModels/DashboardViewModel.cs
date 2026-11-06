using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.App.Models;

namespace RobloxAccountManager.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private int _totalAccountsCount = 3;

    [ObservableProperty]
    private int _activeSessionsCount = 1;

    [ObservableProperty]
    private string _securityStatus = "DPAPI Encrypted (Protected)";

    [ObservableProperty]
    private AccountDisplayModel? _selectedAccount;

    public ObservableCollection<AccountDisplayModel> RecentAccounts { get; } = new();

    public DashboardViewModel()
    {
        LoadMockData();
    }

    private void LoadMockData()
    {
        RecentAccounts.Add(new AccountDisplayModel
        {
            Username = "Valkyrie_Hero",
            DisplayName = "Valkyrie",
            Tag = "Main",
            IsDefault = true,
            LastActive = "2 hours ago"
        });

        RecentAccounts.Add(new AccountDisplayModel
        {
            Username = "RobloxDev_Studio",
            DisplayName = "Studio Dev",
            Tag = "Developer",
            IsDefault = false,
            LastActive = "Yesterday"
        });

        RecentAccounts.Add(new AccountDisplayModel
        {
            Username = "ShadowTrader_99",
            DisplayName = "Trader",
            Tag = "Alt",
            IsDefault = false,
            LastActive = "3 days ago"
        });

        SelectedAccount = RecentAccounts[0];
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
