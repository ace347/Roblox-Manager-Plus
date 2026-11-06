using FluentAssertions;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.Core.Tests;

public class AccountModelTests
{
    [Fact]
    public void NewAccount_HasDefaultValues_AndValidGuid()
    {
        var account = new Account
        {
            Username = "TestPlayer"
        };

        account.Id.Should().NotBeEmpty();
        account.Username.Should().Be("TestPlayer");
        account.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        account.IsDefault.Should().BeFalse();
    }
}
