using FluentAssertions;
using Moq;
using RobloxAccountManager.App.ViewModels;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public class MainViewModelTests
{
    [Fact]
    public void MainViewModel_InitializesWithDashboard()
    {
        var mockService = new Mock<IAccountService>();
        var clientSessions = CreateClientSessionService();
        mockService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Account>());
        var dashVm = new DashboardViewModel(mockService.Object, clientSessions.Object);
        var accVm = new AccountsViewModel(mockService.Object);
        var sessionsVm = new SessionsViewModel(mockService.Object, clientSessions.Object);
        var mainVm = new MainViewModel(dashVm, accVm, sessionsVm);

        mainVm.Title.Should().Be("Secure Roblox Account Manager");
        mainVm.CurrentPage.Should().Be(NavigationPage.Dashboard);
        mainVm.DashboardVm.Should().BeSameAs(dashVm);
        mainVm.AccountsVm.Should().BeSameAs(accVm);
        mainVm.SessionsVm.Should().BeSameAs(sessionsVm);
    }

    [Fact]
    public async Task NavigateCommand_ChangesCurrentPage()
    {
        var mockService = new Mock<IAccountService>();
        var clientSessions = CreateClientSessionService();
        mockService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Account>());
        var dashVm = new DashboardViewModel(mockService.Object, clientSessions.Object);
        var accVm = new AccountsViewModel(mockService.Object);
        var sessionsVm = new SessionsViewModel(mockService.Object, clientSessions.Object);
        var mainVm = new MainViewModel(dashVm, accVm, sessionsVm);

        await mainVm.NavigateCommand.ExecuteAsync(NavigationPage.Accounts);
        mainVm.CurrentPage.Should().Be(NavigationPage.Accounts);
        mainVm.CurrentPageViewModel.Should().BeSameAs(accVm);

        await mainVm.NavigateCommand.ExecuteAsync(NavigationPage.Sessions);
        mainVm.CurrentPage.Should().Be(NavigationPage.Sessions);
        mainVm.CurrentPageViewModel.Should().BeSameAs(sessionsVm);

        await mainVm.NavigateCommand.ExecuteAsync(NavigationPage.Activity);
        mainVm.CurrentPage.Should().Be(NavigationPage.Activity);
        mainVm.CurrentPageViewModel.Should().BeOfType<PageNoticeViewModel>()
            .Which.Title.Should().Be("Activity");

        await mainVm.NavigateCommand.ExecuteAsync(NavigationPage.Security);
        mainVm.CurrentPage.Should().Be(NavigationPage.Security);
        mainVm.CurrentPageViewModel.Should().BeOfType<PageNoticeViewModel>()
            .Which.Title.Should().Be("Security");

        await mainVm.NavigateCommand.ExecuteAsync(NavigationPage.Settings);
        mainVm.CurrentPage.Should().Be(NavigationPage.Settings);
        mainVm.CurrentPageViewModel.Should().BeOfType<PageNoticeViewModel>()
            .Which.Title.Should().Be("Settings");

        await mainVm.NavigateCommand.ExecuteAsync(NavigationPage.Dashboard);
        mainVm.CurrentPage.Should().Be(NavigationPage.Dashboard);
        mainVm.CurrentPageViewModel.Should().BeSameAs(dashVm);
    }

    private static Mock<IClientSessionService> CreateClientSessionService()
    {
        var service = new Mock<IClientSessionService>();
        service.Setup(value => value.GetRunningPlayerProcessCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        service.Setup(value => value.HasUnmanagedPlayerProcessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        service.Setup(value => value.RefreshAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ClientSession>());
        return service;
    }
}
