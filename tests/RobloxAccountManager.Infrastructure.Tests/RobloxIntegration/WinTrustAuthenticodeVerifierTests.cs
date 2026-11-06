using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RobloxAccountManager.Core.Models;
using RobloxAccountManager.Infrastructure.RobloxIntegration;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests.RobloxIntegration;

/// <summary>
/// Comprehensive unit and integration tests for <see cref="WinTrustAuthenticodeVerifier"/>.
/// Tests cover:
/// 1. Missing executable handling
/// 2. Unsigned executable rejection
/// 3. Modified/corrupted binary rejection (tampered digest)
/// 4. Valid signed binary (Windows system binary)
/// 5. Publisher identity validation and mismatch handling
/// </summary>
public sealed class WinTrustAuthenticodeVerifierTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly WinTrustAuthenticodeVerifier _verifier = new(NullLogger<WinTrustAuthenticodeVerifier>.Instance);

    public WinTrustAuthenticodeVerifierTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    // ── 1. Missing Executable ──────────────────────────────────────────────

    [Fact]
    public void VerifyFile_NullOrEmptyPath_ReturnsFileNotFound()
    {
        var resultNull = _verifier.VerifyFile(null!);
        resultNull.IsValid.Should().BeFalse();
        resultNull.Status.Should().Be(AuthenticodeStatus.FileNotFound);

        var resultEmpty = _verifier.VerifyFile("   ");
        resultEmpty.IsValid.Should().BeFalse();
        resultEmpty.Status.Should().Be(AuthenticodeStatus.FileNotFound);
    }

    [Fact]
    public void VerifyFile_NonExistentFile_ReturnsFileNotFound()
    {
        var path = Path.Combine(_tempDir, "DoesNotExist.exe");
        var result = _verifier.VerifyFile(path);

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(AuthenticodeStatus.FileNotFound);
        result.Message.Should().Contain("does not exist");
    }

    // ── 2. Unsigned Executable ─────────────────────────────────────────────

    [Fact]
    public void VerifyFile_UnsignedBinary_ReturnsNotSigned()
    {
        var path = Path.Combine(_tempDir, "UnsignedApp.exe");
        // Write raw bytes representing an unsigned executable file
        File.WriteAllBytes(path, new byte[70_000]);

        var result = _verifier.VerifyFile(path);

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(AuthenticodeStatus.NotSigned);
        result.Message.Should().NotBeNullOrEmpty();
    }

    // ── 3. Modified or Corrupted Executable ─────────────────────────────────

    [Fact]
    public void VerifyFile_TamperedSignedBinary_ReturnsCorruptedOrModified()
    {
        // Find a standard signed Windows system executable
        var systemExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "consent.exe");
        if (!File.Exists(systemExe))
        {
            // Fallback to svchost or notepad if consent.exe is missing
            systemExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "svchost.exe");
        }

        if (!File.Exists(systemExe))
        {
            return; // Skip on non-standard Windows installations
        }

        // Copy the genuine signed binary to temp folder
        var tamperedPath = Path.Combine(_tempDir, "Tampered.exe");
        var bytes = File.ReadAllBytes(systemExe);

        // Intentionally tamper with middle bytes of the executable payload to corrupt the digest
        bytes[200] = (byte)(bytes[200] ^ 0xFF);
        File.WriteAllBytes(tamperedPath, bytes);

        var result = _verifier.VerifyFile(tamperedPath);

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(AuthenticodeStatus.CorruptedOrModified);
        result.Message.Should().Contain("modified or corrupted");
    }

    // ── 4. Valid Signed Executable ─────────────────────────────────────────

    [Fact]
    public void VerifyFile_ValidSignedWindowsBinary_ReturnsValid()
    {
        var systemExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "consent.exe");
        if (!File.Exists(systemExe))
        {
            systemExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "svchost.exe");
        }

        if (!File.Exists(systemExe))
        {
            return;
        }

        // Verify without publisher constraint
        var result = _verifier.VerifyFile(systemExe);

        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(AuthenticodeStatus.Valid);
        result.RawHResult.Should().Be(0);
        result.SignerSubject.Should().Contain("Microsoft");
    }

    // ── 5. Publisher Identity Matching ─────────────────────────────────────

    [Fact]
    public void VerifyFile_ExpectedPublisherMatched_ReturnsValid()
    {
        var systemExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "consent.exe");
        if (!File.Exists(systemExe))
        {
            systemExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "svchost.exe");
        }

        if (!File.Exists(systemExe))
        {
            return;
        }

        // Expect Microsoft Corporation
        var result = _verifier.VerifyFile(systemExe, ["Microsoft Corporation", "Microsoft Windows"]);

        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(AuthenticodeStatus.Valid);
    }

    [Fact]
    public void VerifyFile_PublisherMismatch_ReturnsPublisherMismatch()
    {
        var systemExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "consent.exe");
        if (!File.Exists(systemExe))
        {
            systemExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "svchost.exe");
        }

        if (!File.Exists(systemExe))
        {
            return;
        }

        // Expect Roblox Corporation on a Microsoft binary -> must fail with PublisherMismatch
        var result = _verifier.VerifyFile(systemExe, ["Roblox Corporation"]);

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(AuthenticodeStatus.PublisherMismatch);
        result.Message.Should().Contain("did not match expected identity");
        result.SignerSubject.Should().Contain("Microsoft");
    }
}
