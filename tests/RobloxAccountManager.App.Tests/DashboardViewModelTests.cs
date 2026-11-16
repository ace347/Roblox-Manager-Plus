using FluentAssertions;
using Moq;
using RobloxAccountManager.App.ViewModels;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public class DashboardViewModelTests
{
    private static Mock<IClientSessionService> CreateSessionService(int processCount = 0)
    {
        var sessions = new Mock<IClientSessionService>();
        sessions.Setup(service => service.GetRunningPlayerProcessCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(processCount);
        sessions.Setup(service => service.HasUnmanagedPlayerProcessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        return sessions;
    }

    [Fact]
    public async Task RefreshAsync_PopulatesAccountsFromService()
    {
        var mockService = new Mock<IAccountService>();
        var sessionService = CreateSessionService(processCount: 1);
        mockService.Setup(s => s.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>
            {
                new()
                {
                    Username = "TestValkyrie",
                    DisplayName = "Valkyrie",
                    Tag = "Main",
                    IsDefault = true,
                    EncryptedSecret = [1, 2, 3]
                }
            });

        var vm = new DashboardViewModel(mockService.Object, sessionService.Object);
        await vm.RefreshAsync();

        vm.TotalAccountsCount.Should().Be(1);
        vm.ActiveSessionsCount.Should().Be(1);
        vm.StoredSecretsCount.Should().Be(1);
        vm.RecentAccounts.Should().HaveCount(1);
        vm.SelectedAccount.Should().NotBeNull();
        vm.SelectedAccount!.Username.Should().Be("TestValkyrie");
        vm.IsEmpty.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_WhenDatabaseIsEmpty_SetsEmptyState()
    {
        var mockService = new Mock<IAccountService>();
        var sessionService = CreateSessionService();
        mockService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Account>());
        var vm = new DashboardViewModel(mockService.Object, sessionService.Object);

        await vm.RefreshAsync();

        vm.TotalAccountsCount.Should().Be(0);
        vm.IsEmpty.Should().BeTrue();
        vm.IsLoading.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_ReportsLoadingUntilDatabaseReadCompletes()
    {
        var response = new TaskCompletionSource<IReadOnlyList<Account>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mockService = new Mock<IAccountService>();
        var sessionService = CreateSessionService();
        mockService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .Returns(response.Task);
        var vm = new DashboardViewModel(mockService.Object, sessionService.Object);

        var refresh = vm.RefreshAsync();

        vm.IsLoading.Should().BeTrue();
        response.SetResult(Array.Empty<Account>());
        await refresh;

        vm.IsLoading.Should().BeFalse();
        vm.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_WhenDatabaseFails_SetsErrorStateWithoutExposingException()
    {
        var mockService = new Mock<IAccountService>();
        var sessionService = CreateSessionService();
        mockService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("private database path"));
        var vm = new DashboardViewModel(mockService.Object, sessionService.Object);

        await vm.RefreshAsync();

        vm.IsLoading.Should().BeFalse();
        vm.IsEmpty.Should().BeFalse();
        vm.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        vm.ErrorMessage.Should().NotContain("private database path");
    }

    [Fact]
    public async Task QuickLaunchCommand_LaunchesSelectedSavedAccountWithUnverifiedSigninMessage()
    {
        var account = new Account { Username = "SelectedProfile", IsDefault = true };
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);
        var sessions = CreateSessionService();
        sessions.SetupSequence(service => service.GetRunningPlayerProcessCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .ReturnsAsync(1);
        sessions.Setup(service => service.LaunchAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientSession
            {
                AccountId = account.Id,
                AccountUsername = account.Username,
                Status = ClientSessionStatus.Running
            });
        var viewModel = new DashboardViewModel(accountService.Object, sessions.Object);
        await viewModel.RefreshAsync();

        await viewModel.QuickLaunchCommand.ExecuteAsync(null);

        sessions.Verify(service => service.LaunchAsync(account.Id, It.IsAny<CancellationToken>()), Times.Once);
        viewModel.ActiveSessionsCount.Should().Be(1);
        viewModel.LaunchMessage.Should().Contain("sign-in must be checked");
    }

    [Fact]
    public async Task RefreshAsync_WhenProcessInspectionFails_KeepsAccountCountAndDisablesLaunch()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Account { Username = "SavedProfile" }]);
        var sessions = CreateSessionService();
        sessions.Setup(service => service.GetRunningPlayerProcessCountAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("process inspection unavailable"));
        var viewModel = new DashboardViewModel(accountService.Object, sessions.Object);

        await viewModel.RefreshAsync();

        viewModel.TotalAccountsCount.Should().Be(1);
        viewModel.ActiveSessionsCount.Should().BeNull();
        viewModel.IsProcessCountAvailable.Should().BeFalse();
        viewModel.ErrorMessage.Should().Contain("inspect local Roblox Player processes");
        viewModel.QuickLaunchCommand.CanExecute(null).Should().BeFalse();
    }
}
