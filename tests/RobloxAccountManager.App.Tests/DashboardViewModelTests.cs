using FluentAssertions;
using RobloxAccountManager.App.ViewModels;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public class DashboardViewModelTests
{
    [Fact]
    public void DashboardViewModel_InitializesMockAccountsAndStats()
    {
        var vm = new DashboardViewModel();

        vm.TotalAccountsCount.Should().Be(3);
        vm.RecentAccounts.Should().HaveCount(3);
        vm.SelectedAccount.Should().NotBeNull();
        vm.SelectedAccount!.Username.Should().Be("Valkyrie_Hero");
        vm.SecurityStatus.Should().Contain("DPAPI");
    }
}
