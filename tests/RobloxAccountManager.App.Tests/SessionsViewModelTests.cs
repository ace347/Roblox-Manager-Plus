using FluentAssertions;
using Moq;
using RobloxAccountManager.App.ViewModels;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public sealed class SessionsViewModelTests
{
    [Fact]
    public async Task RefreshAsync_LoadsSavedProfilesAndPersistedSessions()
    {
        var account = new Account { Username = "ProfileOne", IsDefault = true };
        var session = new ClientSession
        {
            AccountId = account.Id,
            AccountUsername = account.Username,
            ProcessId = 1234,
            ExecutablePath = @"C:\Roblox\RobloxPlayerBeta.exe",
            Status = ClientSessionStatus.Running
        };
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);
        var sessionService = CreateSessionService([session]);
        var viewModel = new SessionsViewModel(accountService.Object, sessionService.Object);

        await viewModel.RefreshAsync();

        viewModel.Accounts.Should().ContainSingle();
        viewModel.SelectedAccount!.Id.Should().Be(account.Id);
        viewModel.Sessions.Should().ContainSingle();
        viewModel.Sessions[0].ProcessId.Should().Be(1234);
        viewModel.Sessions[0].AuthenticationStatus.Should().Contain("Unverified");
        viewModel.IsEmpty.Should().BeFalse();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LaunchSelectedCommand_AssociatesSavedAccountAndKeepsAuthenticationUnverified()
    {
        var account = new Account { Username = "ProfileOne", IsDefault = true };
        var session = new ClientSession
        {
            AccountId = account.Id,
            AccountUsername = account.Username,
            ProcessId = 1234,
            ExecutablePath = @"C:\Roblox\RobloxPlayerBeta.exe",
            Status = ClientSessionStatus.Running
        };
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);
        var sessionService = CreateSessionService();
        sessionService.SetupSequence(service => service.RefreshAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ClientSession>())
            .ReturnsAsync([session]);
        sessionService.Setup(service => service.LaunchAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var viewModel = new SessionsViewModel(accountService.Object, sessionService.Object);
        await viewModel.RefreshAsync();

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        sessionService.Verify(service => service.LaunchAsync(account.Id, It.IsAny<CancellationToken>()), Times.Once);
        viewModel.StatusMessage.Should().Contain("unverified");
        viewModel.Sessions.Should().ContainSingle(item => item.ProcessId == 1234);
    }

    [Fact]
    public async Task RefreshAsync_AllowsLaunchWhenAnUnmanagedPlayerIsRunning()
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Account { Username = "ProfileOne" }]);
        var sessionService = CreateSessionService();
        sessionService.Setup(service => service.HasUnmanagedPlayerProcessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var viewModel = new SessionsViewModel(accountService.Object, sessionService.Object);

        await viewModel.RefreshAsync();

        viewModel.HasUnmanagedPlayerProcess.Should().BeTrue();
        viewModel.LaunchSelectedCommand.CanExecute(null).Should().BeTrue();
    }

    private static Mock<IClientSessionService> CreateSessionService(IEnumerable<ClientSession>? sessions = null)
    {
        var service = new Mock<IClientSessionService>();
        service.Setup(value => value.RefreshAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions?.ToArray() ?? Array.Empty<ClientSession>());
        service.Setup(value => value.HasUnmanagedPlayerProcessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        service.Setup(value => value.LaunchAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid accountId, CancellationToken _) => new ClientSession
            {
                AccountId = accountId,
                AccountUsername = "ProfileOne",
                Status = ClientSessionStatus.LaunchFailed,
                LastError = "Test launch was not configured."
            });
        return service;
    }
}