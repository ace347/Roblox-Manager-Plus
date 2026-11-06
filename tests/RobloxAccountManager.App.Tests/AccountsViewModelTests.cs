using FluentAssertions;
using Moq;
using RobloxAccountManager.App.ViewModels;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public class AccountsViewModelTests
{
    [Fact]
    public async Task LoadAccountsAsync_PopulatesAccountsAndSetsEmptyState()
    {
        var mockService = new Mock<IAccountService>();
        mockService.Setup(s => s.SearchAccountsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>
            {
                new() { Username = "Player1", DisplayName = "One", Tag = "Main", IsDefault = true }
            });

        var vm = new AccountsViewModel(mockService.Object);
        await vm.LoadAccountsAsync();

        vm.Accounts.Should().HaveCount(1);
        vm.IsEmpty.Should().BeFalse();
        vm.SelectedAccount.Should().NotBeNull();
        vm.SelectedAccount!.Username.Should().Be("Player1");
    }

    [Fact]
    public void OpenAddDialog_ResetsFieldsAndSetsDialogOpen()
    {
        var mockService = new Mock<IAccountService>();
        var vm = new AccountsViewModel(mockService.Object);

        vm.OpenAddDialogCommand.Execute(null);

        vm.IsDialogOpen.Should().BeTrue();
        vm.IsEditMode.Should().BeFalse();
        vm.FormUsername.Should().BeEmpty();
    }
}
