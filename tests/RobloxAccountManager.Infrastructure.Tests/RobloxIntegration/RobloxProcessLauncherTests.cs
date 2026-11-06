using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
    // Temporary directory used for creating test .exe stubs.
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public RobloxProcessLauncherTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

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
        // Do NOT create the file.
        RobloxProcessLauncher.ValidateExecutablePath(path).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateExecutablePath_ValidPath_ReturnsNull()
    {
        // Create a stub file of sufficient size.
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
        var locator = new RobloxClientLocator(NullLogger<RobloxClientLocator>.Instance);
        var launcher = new RobloxProcessLauncher(locator, NullLogger<RobloxProcessLauncher>.Instance);

        var installation = new RobloxInstallation
        {
            ExecutablePath = Path.Combine(_tempDir, "RobloxPlayerBeta.exe"),
            Version = "version-nonexistent",
        };
        // The file is NOT created — it must not exist.

        var result = await launcher.LaunchAsync(installation);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().NotBeNullOrEmpty();
        result.ProcessId.Should().BeNull();
    }

    [Fact]
    public async Task LaunchAsync_InvalidPlaceUrlScheme_ReturnsFailed()
    {
        // Even with a valid exe stub, an http URL must be rejected before launch.
        var exePath = Path.Combine(_tempDir, "RobloxPlayerBeta.exe");
        File.WriteAllBytes(exePath, new byte[70_000]);

        var locator = new RobloxClientLocator(NullLogger<RobloxClientLocator>.Instance);
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
        var locator = new RobloxClientLocator(NullLogger<RobloxClientLocator>.Instance);
        var launcher = new RobloxProcessLauncher(locator, NullLogger<RobloxProcessLauncher>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            launcher.LaunchAsync(null!));
    }

    // ── LaunchAsync — controlled executable launch ─────────────────────────

    [Fact]
    public async Task LaunchAsync_ValidControlledExe_Succeeds()
    {
        // Use cmd.exe (always present on Windows) renamed to RobloxPlayerBeta.exe
        // for a fully controlled launch test.  We pass /C exit to ensure it terminates quickly.
        var cmdPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

        if (!File.Exists(cmdPath))
        {
            // Skip on non-Windows environments.
            return;
        }

        // Copy cmd.exe as RobloxPlayerBeta.exe into temp dir.
        var exePath = Path.Combine(_tempDir, "RobloxPlayerBeta.exe");
        File.Copy(cmdPath, exePath, overwrite: true);

        var locator = new RobloxClientLocator(NullLogger<RobloxClientLocator>.Instance);
        var launcher = new RobloxProcessLauncher(locator, NullLogger<RobloxProcessLauncher>.Instance);

        var installation = new RobloxInstallation
        {
            ExecutablePath = exePath,
            Version = "version-test",
        };

        // No place URL — just launch the stub.
        var result = await launcher.LaunchAsync(installation, placeUrl: null);

        result.Success.Should().BeTrue();
        result.ProcessId.Should().BePositive();

        // Clean up the launched process quickly.
        try
        {
            var process = System.Diagnostics.Process.GetProcessById(result.ProcessId!.Value);
            process.Kill(entireProcessTree: true);
        }
        catch { /* process may have already exited */ }
    }
}

