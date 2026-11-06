using FluentAssertions;
using Moq;
using RobloxAccountManager.App.ViewModels;
using RobloxAccountManager.Application.Services;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public class MainViewModelTests
{
    [Fact]
    public void MainViewModel_InitializesWithDashboard()
    {
        var mockService = new Mock<IAccountService>();
        var dashVm = new DashboardViewModel(mockService.Object);
        var accVm = new AccountsViewModel(mockService.Object);
        var mainVm = new MainViewModel(dashVm, accVm);

        mainVm.Title.Should().Be("Secure Roblox Account Manager");
        mainVm.CurrentPage.Should().Be(NavigationPage.Dashboard);
        mainVm.DashboardVm.Should().BeSameAs(dashVm);
        mainVm.AccountsVm.Should().BeSameAs(accVm);
    }

    [Fact]
    public async Task NavigateCommand_ChangesCurrentPage()
    {
        var mockService = new Mock<IAccountService>();
        var dashVm = new DashboardViewModel(mockService.Object);
        var accVm = new AccountsViewModel(mockService.Object);
        var mainVm = new MainViewModel(dashVm, accVm);

        await mainVm.NavigateCommand.ExecuteAsync("Accounts");
        mainVm.CurrentPage.Should().Be(NavigationPage.Accounts);

        await mainVm.NavigateCommand.ExecuteAsync("Settings");
        mainVm.CurrentPage.Should().Be(NavigationPage.Settings);
    }
}
