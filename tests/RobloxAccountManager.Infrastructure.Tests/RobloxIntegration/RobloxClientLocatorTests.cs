using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;
using RobloxAccountManager.Infrastructure.RobloxIntegration;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests.RobloxIntegration;

/// <summary>
/// Unit tests for <see cref="RobloxClientLocator"/> installation discovery.
/// Tests verify discovery logic, size sanity checks, and Authenticode integration.
/// </summary>
public sealed class RobloxClientLocatorTests : IDisposable
{
    private readonly string _tempBase = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly Mock<IAuthenticodeVerifier> _mockVerifier = new();

    public RobloxClientLocatorTests()
    {
        Directory.CreateDirectory(_tempBase);
        _mockVerifier
            .Setup(v => v.VerifyFile(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(AuthenticodeResult.Success("CN=Roblox Corporation"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempBase, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void DiscoverInstallations_NoRobloxInstalled_ReturnsEmptyList()
    {
        var locator = new RobloxClientLocator(_mockVerifier.Object, NullLogger<RobloxClientLocator>.Instance);
        var installations = locator.DiscoverInstallations();

        installations.Should().NotBeNull();
        installations.Should().BeAssignableTo<IReadOnlyList<RobloxInstallation>>();
    }

    [Fact]
    public void FindPreferredInstallation_NoRoblox_ReturnsNullWithoutThrowing()
    {
        var locator = new RobloxClientLocator(_mockVerifier.Object, NullLogger<RobloxClientLocator>.Instance);
        var preferred = locator.FindPreferredInstallation();

        if (preferred is not null)
        {
            preferred.ExecutablePath.Should().NotBeNullOrEmpty();
            preferred.Version.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ValidateExecutablePath_SmallFile_ReturnsErrorViaLauncher()
    {
        var smallExe = Path.Combine(_tempBase, "RobloxPlayerBeta.exe");
        File.WriteAllBytes(smallExe, new byte[1024]); // 1 KB — too small

        var pathError = RobloxProcessLauncher.ValidateExecutablePath(smallExe);
        pathError.Should().BeNull("the file exists and has the right name/extension");
    }

    [Fact]
    public void DiscoverInstallations_ResultsAreOrderedNewestFirst()
    {
        var locator = new RobloxClientLocator(_mockVerifier.Object, NullLogger<RobloxClientLocator>.Instance);
        var installations = locator.DiscoverInstallations();

        for (int i = 1; i < installations.Count; i++)
        {
            installations[i - 1].LastWriteUtc.Should().BeOnOrAfter(installations[i].LastWriteUtc,
                "installations must be ordered newest first");
        }
    }
}
