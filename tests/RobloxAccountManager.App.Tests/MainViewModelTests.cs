using FluentAssertions;
using RobloxAccountManager.App.ViewModels;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public class MainViewModelTests
{
    [Fact]
    public void MainViewModel_InitializesWithDefaults()
    {
        var vm = new MainViewModel();
        vm.Title.Should().Be("Secure Roblox Account Manager");
        vm.StatusMessage.Should().Be("Ready");
    }
}
