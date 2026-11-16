using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RobloxAccountManager.App.ViewModels;

public enum NavigationPage
{
    Dashboard,
    Accounts,
    Sessions,
    Activity,
    Security,
    Settings
}

public partial class MainViewModel : ObservableObject
{
    private readonly PageNoticeViewModel _activityPage = new(
        "Activity",
        "Activity history is not available. The current application does not record client launch activity.");
    private readonly PageNoticeViewModel _securityPage = new(
        "Security",
        "The optional credential vault uses Windows DPAPI for secrets stored through its API. The current UI does not collect credentials or switch sessions.");
    private readonly PageNoticeViewModel _settingsPage = new(
        "Settings",
        "There are no user-configurable settings in this version.");

    [ObservableProperty]
    private string _title = "Secure Roblox Account Manager";

    [ObservableProperty]
    private NavigationPage _currentPage = NavigationPage.Dashboard;

    [ObservableProperty]
    private DashboardViewModel _dashboardVm;

    [ObservableProperty]
    private AccountsViewModel _accountsVm;

    [ObservableProperty]
    private SessionsViewModel _sessionsVm;

    public MainViewModel(DashboardViewModel dashboardVm, AccountsViewModel accountsVm, SessionsViewModel sessionsVm)
    {
        _dashboardVm = dashboardVm;
        _accountsVm = accountsVm;
        _sessionsVm = sessionsVm;
    }

    public object CurrentPageViewModel => CurrentPage switch
    {
        NavigationPage.Dashboard => DashboardVm,
        NavigationPage.Accounts => AccountsVm,
        NavigationPage.Sessions => SessionsVm,
        NavigationPage.Activity => _activityPage,
        NavigationPage.Security => _securityPage,
        NavigationPage.Settings => _settingsPage,
        _ => _activityPage
    };

    partial void OnCurrentPageChanged(NavigationPage value)
    {
        OnPropertyChanged(nameof(CurrentPageViewModel));
    }

    [RelayCommand]
    private async Task NavigateAsync(NavigationPage page)
    {
        if (!Enum.IsDefined(page))
        {
            return;
        }

        CurrentPage = page;
        if (page == NavigationPage.Dashboard)
        {
            await DashboardVm.RefreshAsync();
        }
        else if (page == NavigationPage.Accounts)
        {
            await AccountsVm.LoadAccountsAsync();
        }
        else if (page == NavigationPage.Sessions)
        {
            await SessionsVm.RefreshAsync();
        }
    }
}

public sealed record PageNoticeViewModel(string Title, string Message);
