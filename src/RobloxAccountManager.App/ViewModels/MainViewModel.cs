using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RobloxAccountManager.App.ViewModels;

public enum NavigationPage
{
    Dashboard,
    Accounts,
    Activity,
    Security,
    Settings
}

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Secure Roblox Account Manager";

    [ObservableProperty]
    private NavigationPage _currentPage = NavigationPage.Dashboard;

    [ObservableProperty]
    private DashboardViewModel _dashboardVm;

    [ObservableProperty]
    private AccountsViewModel _accountsVm;

    public MainViewModel(DashboardViewModel dashboardVm, AccountsViewModel accountsVm)
    {
        _dashboardVm = dashboardVm;
        _accountsVm = accountsVm;
    }

    [RelayCommand]
    private async Task NavigateAsync(string page)
    {
        if (Enum.TryParse<NavigationPage>(page, true, out var navPage))
        {
            CurrentPage = navPage;
            if (navPage == NavigationPage.Dashboard)
            {
                await DashboardVm.RefreshAsync();
            }
            else if (navPage == NavigationPage.Accounts)
            {
                await AccountsVm.LoadAccountsAsync();
            }
        }
    }
}
