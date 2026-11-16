using FluentAssertions;
using RobloxAccountManager.Core.Models;
using RobloxAccountManager.Infrastructure.RobloxIntegration;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests.RobloxIntegration;

public sealed class RobloxProcessInspectorTests
{
    [Fact]
    public void Inspect_CurrentTestProcess_ReturnsItsActualIdentity()
    {
        var result = new RobloxProcessInspector().Inspect(Environment.ProcessId);

        result.Status.Should().Be(ProcessInspectionStatus.Running);
        result.Snapshot.Should().NotBeNull();
        result.Snapshot!.ProcessId.Should().Be(Environment.ProcessId);
        result.Snapshot.StartedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        Path.IsPathFullyQualified(result.Snapshot.ExecutablePath).Should().BeTrue();
    }

    [Fact]
    public void Matches_RejectsReusedPidOrDifferentExecutable()
    {
        var expected = new ClientProcessSnapshot(
            1234,
            new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc),
            @"C:\Roblox\RobloxPlayerBeta.exe");

        RobloxProcessInspector.Matches(expected, expected).Should().BeTrue();
        RobloxProcessInspector.Matches(
            expected with { StartedAtUtc = expected.StartedAtUtc.AddSeconds(3) }, expected).Should().BeFalse();
        RobloxProcessInspector.Matches(
            expected with { ExecutablePath = @"C:\Other\RobloxPlayerBeta.exe" }, expected).Should().BeFalse();
    }
}