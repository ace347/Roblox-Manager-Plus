using FluentAssertions;
using Moq;
using RobloxAccountManager.App.ViewModels;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public class DashboardViewModelTests
{
    [Fact]
    public async Task RefreshAsync_PopulatesAccountsFromService()
    {
        var mockService = new Mock<IAccountService>();
        mockService.Setup(s => s.GetAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>
            {
                new() { Username = "TestValkyrie", DisplayName = "Valkyrie", Tag = "Main", IsDefault = true }
            });

        var vm = new DashboardViewModel(mockService.Object);
        await vm.RefreshAsync();

        vm.TotalAccountsCount.Should().Be(1);
        vm.RecentAccounts.Should().HaveCount(1);
        vm.SelectedAccount.Should().NotBeNull();
        vm.SelectedAccount!.Username.Should().Be("TestValkyrie");
    }
}
