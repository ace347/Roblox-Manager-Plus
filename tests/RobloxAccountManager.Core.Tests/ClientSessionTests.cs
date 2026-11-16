using FluentAssertions;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.Core.Tests;

public sealed class ClientSessionTests
{
    [Theory]
    [InlineData(ClientSessionStatus.Launching, true)]
    [InlineData(ClientSessionStatus.Running, true)]
    [InlineData(ClientSessionStatus.StatusUnavailable, true)]
    [InlineData(ClientSessionStatus.Exited, false)]
    [InlineData(ClientSessionStatus.LaunchFailed, false)]
    public void IsActive_ReflectsManagedProcessState(ClientSessionStatus status, bool expected)
    {
        var session = new ClientSession { Status = status };

        session.IsActive.Should().Be(expected);
        session.AuthenticationStatus.Should().Contain("Unverified");
    }
}