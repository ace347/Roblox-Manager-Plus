using FluentAssertions;
using RobloxAccountManager.App.ViewModels;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public class MainViewModelTests
{
    [Fact]
    public void MainViewModel_InitializesWithDashboard()
    {
        var dashVm = new DashboardViewModel();
        var mainVm = new MainViewModel(dashVm);

        mainVm.Title.Should().Be("Secure Roblox Account Manager");
        mainVm.CurrentPage.Should().Be(NavigationPage.Dashboard);
        mainVm.DashboardVm.Should().BeSameAs(dashVm);
    }

    [Fact]
    public void NavigateCommand_ChangesCurrentPage()
    {
        var dashVm = new DashboardViewModel();
        var mainVm = new MainViewModel(dashVm);

        mainVm.NavigateCommand.Execute("Accounts");
        mainVm.CurrentPage.Should().Be(NavigationPage.Accounts);

        mainVm.NavigateCommand.Execute("Settings");
        mainVm.CurrentPage.Should().Be(NavigationPage.Settings);
    }
}
