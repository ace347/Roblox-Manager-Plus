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

    public MainViewModel(DashboardViewModel dashboardVm)
    {
        _dashboardVm = dashboardVm;
    }

    [RelayCommand]
    private void Navigate(string page)
    {
        if (Enum.TryParse<NavigationPage>(page, true, out var navPage))
        {
            CurrentPage = navPage;
        }
    }
}
