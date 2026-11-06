using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;
using RobloxAccountManager.Infrastructure.RobloxIntegration;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests.RobloxIntegration;

/// <summary>
/// Unit tests for <see cref="RobloxProcessLauncher"/> path/URL validation and launch logic.
/// Real Roblox processes are never started; tests use controlled temporary executables where needed.
/// </summary>
public sealed class RobloxProcessLauncherTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly Mock<IAuthenticodeVerifier> _mockVerifier = new();

    public RobloxProcessLauncherTests()
    {
        Directory.CreateDirectory(_tempDir);
        _mockVerifier
            .Setup(v => v.VerifyFile(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(AuthenticodeResult.Success("CN=Roblox Corporation"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    private RobloxClientLocator CreateLocator() =>
        new(_mockVerifier.Object, NullLogger<RobloxClientLocator>.Instance);

    // ── ValidateExecutablePath ─────────────────────────────────────────────

    [Fact]
    public void ValidateExecutablePath_NullOrEmpty_ReturnsError()
    {
        RobloxProcessLauncher.ValidateExecutablePath(null).Should().NotBeNullOrEmpty();
        RobloxProcessLauncher.ValidateExecutablePath("").Should().NotBeNullOrEmpty();
        RobloxProcessLauncher.ValidateExecutablePath("   ").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateExecutablePath_RelativePath_ReturnsError()
    {
        var result = RobloxProcessLauncher.ValidateExecutablePath(@"Roblox\Versions\version-abc\RobloxPlayerBeta.exe");
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateExecutablePath_NonExeExtension_ReturnsError()
    {
        var path = Path.Combine(_tempDir, "RobloxPlayerBeta.bat");
        File.WriteAllBytes(path, new byte[70_000]);
        RobloxProcessLauncher.ValidateExecutablePath(path).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateExecutablePath_WrongExecutableName_ReturnsError()
    {
        var path = Path.Combine(_tempDir, "SomethingElse.exe");
        File.WriteAllBytes(path, new byte[70_000]);
        RobloxProcessLauncher.ValidateExecutablePath(path).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateExecutablePath_FileDoesNotExist_ReturnsError()
    {
        var path = Path.Combine(_tempDir, "RobloxPlayerBeta.exe");
        RobloxProcessLauncher.ValidateExecutablePath(path).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateExecutablePath_ValidPath_ReturnsNull()
    {
        var path = Path.Combine(_tempDir, "RobloxPlayerBeta.exe");
        File.WriteAllBytes(path, new byte[70_000]);

        var result = RobloxProcessLauncher.ValidateExecutablePath(path);
        result.Should().BeNull("a valid, existing RobloxPlayerBeta.exe should pass validation");
    }

    // ── ValidatePlaceUrl ───────────────────────────────────────────────────

    [Fact]
    public void ValidatePlaceUrl_NullOrEmpty_ReturnsError()
    {
        RobloxProcessLauncher.ValidatePlaceUrl(null!).Should().NotBeNullOrEmpty();
        RobloxProcessLauncher.ValidatePlaceUrl("").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidatePlaceUrl_HttpScheme_ReturnsError()
    {
        var result = RobloxProcessLauncher.ValidatePlaceUrl("https://www.roblox.com/games/12345");
        result.Should().NotBeNullOrEmpty("http/https scheme must be rejected");
    }

    [Fact]
    public void ValidatePlaceUrl_JavaScriptScheme_ReturnsError()
    {
        var result = RobloxProcessLauncher.ValidatePlaceUrl("javascript:alert(1)");
        result.Should().NotBeNullOrEmpty("javascript: scheme must be rejected");
    }

    [Fact]
    public void ValidatePlaceUrl_FileScheme_ReturnsError()
    {
        var result = RobloxProcessLauncher.ValidatePlaceUrl("file:///C:/Windows/System32/cmd.exe");
        result.Should().NotBeNullOrEmpty("file: scheme must be rejected");
    }

    [Fact]
    public void ValidatePlaceUrl_NotAbsoluteUri_ReturnsError()
    {
        var result = RobloxProcessLauncher.ValidatePlaceUrl("experiences/start?placeId=12345");
        result.Should().NotBeNullOrEmpty("relative URIs must be rejected");
    }

    [Fact]
    public void ValidatePlaceUrl_ValidRobloxUri_ReturnsNull()
    {
        var result = RobloxProcessLauncher.ValidatePlaceUrl("roblox://experiences/start?placeId=12345");
        result.Should().BeNull("valid roblox:// URIs should be accepted");
    }

    // ── LaunchAsync — missing Roblox ───────────────────────────────────────

    [Fact]
    public async Task LaunchAsync_NonExistentExecutable_ReturnsFailed()
    {
        var locator = CreateLocator();
        var launcher = new RobloxProcessLauncher(locator, NullLogger<RobloxProcessLauncher>.Instance);

        var installation = new RobloxInstallation
        {
            ExecutablePath = Path.Combine(_tempDir, "RobloxPlayerBeta.exe"),
            Version = "version-nonexistent",
        };

        var result = await launcher.LaunchAsync(installation);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().NotBeNullOrEmpty();
        result.ProcessId.Should().BeNull();
    }

    [Fact]
    public async Task LaunchAsync_InvalidPlaceUrlScheme_ReturnsFailed()
    {
        var exePath = Path.Combine(_tempDir, "RobloxPlayerBeta.exe");
        File.WriteAllBytes(exePath, new byte[70_000]);

        var locator = CreateLocator();
        var launcher = new RobloxProcessLauncher(locator, NullLogger<RobloxProcessLauncher>.Instance);

        var installation = new RobloxInstallation
        {
            ExecutablePath = exePath,
            Version = "version-test",
        };

        var result = await launcher.LaunchAsync(installation, "https://evil.example.com");

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("scheme");
    }

    [Fact]
    public async Task LaunchAsync_NullInstallation_ThrowsArgumentNull()
    {
        var locator = CreateLocator();
        var launcher = new RobloxProcessLauncher(locator, NullLogger<RobloxProcessLauncher>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            launcher.LaunchAsync(null!));
    }

    // ── LaunchAsync — controlled executable launch ─────────────────────────

    [Fact]
    public async Task LaunchAsync_ValidControlledExe_Succeeds()
    {
        var cmdPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

        if (!File.Exists(cmdPath))
        {
            return;
        }

        var exePath = Path.Combine(_tempDir, "RobloxPlayerBeta.exe");
        File.Copy(cmdPath, exePath, overwrite: true);

        var locator = CreateLocator();
        var launcher = new RobloxProcessLauncher(locator, NullLogger<RobloxProcessLauncher>.Instance);

        var installation = new RobloxInstallation
        {
            ExecutablePath = exePath,
            Version = "version-test",
        };

        var result = await launcher.LaunchAsync(installation, placeUrl: null);

        result.Success.Should().BeTrue();
        result.ProcessId.Should().BePositive();

        try
        {
            var process = System.Diagnostics.Process.GetProcessById(result.ProcessId!.Value);
            process.Kill(entireProcessTree: true);
        }
        catch { /* process may have already exited */ }
    }
}
