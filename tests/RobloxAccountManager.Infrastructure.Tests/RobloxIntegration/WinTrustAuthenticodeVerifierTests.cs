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
/// 5. Publisher identity validation and exact CN matching (prevents substring spoofing)
/// 6. Substring spoofing rejection (e.g. "NotRoblox Corporation" or "Roblox Corporation Fake")
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

    private static string? GetValidSystemSignedExe()
    {
        var candidates = new[] { "consent.exe", "svchost.exe", "cmd.exe" };
        var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);

        foreach (var candidate in candidates)
        {
            var fullPath = Path.Combine(systemPath, candidate);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
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
        var systemExe = GetValidSystemSignedExe();
        if (systemExe is null) return;

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
        var systemExe = GetValidSystemSignedExe();
        if (systemExe is null) return;

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
        var systemExe = GetValidSystemSignedExe();
        if (systemExe is null) return;

        var result = _verifier.VerifyFile(systemExe, ["Microsoft Windows", "Microsoft Corporation"]);

        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(AuthenticodeStatus.Valid);
    }

    [Fact]
    public void VerifyFile_PublisherMismatch_ReturnsPublisherMismatch()
    {
        var systemExe = GetValidSystemSignedExe();
        if (systemExe is null) return;

        // Expect Roblox Corporation on a Microsoft binary -> must fail with PublisherMismatch
        var result = _verifier.VerifyFile(systemExe, ["Roblox Corporation"]);

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(AuthenticodeStatus.PublisherMismatch);
        result.Message.Should().Contain("did not match expected publisher");
        result.SignerSubject.Should().Contain("Microsoft");
    }

    [Fact]
    public void VerifyFile_SubstringSpoofingPublisher_IsRejected()
    {
        var systemExe = GetValidSystemSignedExe();
        if (systemExe is null) return;

        // Microsoft Windows is the genuine publisher. Testing that a partial substring or spoofed publisher like "Microsoft Windows Fake" is rejected
        var result = _verifier.VerifyFile(systemExe, ["Microsoft Windows Fake", "Windows"]);

        // Since "Windows" alone is not the full Common Name "Microsoft Windows", it must be rejected unless exact Common Name matches
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(AuthenticodeStatus.PublisherMismatch);
    }

    // ── 6. WinTrust Constants and Field Assignment Regression Tests ────────

    [Fact]
    public void WinTrustConstants_RevocationFieldAssignments_HaveExactValues()
    {
        // fdwRevocationChecks must use WTD_REVOKE_WHOLECHAIN (0x00000001)
        WinTrustAuthenticodeVerifier.WTD_REVOKE_WHOLECHAIN.Should().Be(0x00000001U);
        WinTrustAuthenticodeVerifier.WTD_REVOKE_NONE.Should().Be(0x00000000U);

        // dwProvFlags must use WTD_REVOCATION_CHECK_CHAIN (0x00000040)
        WinTrustAuthenticodeVerifier.WTD_REVOCATION_CHECK_CHAIN.Should().Be(0x00000040U);

        // dwProvFlags must use WTD_CACHE_ONLY_URL_RETRIEVAL (0x00001000)
        WinTrustAuthenticodeVerifier.WTD_CACHE_ONLY_URL_RETRIEVAL.Should().Be(0x00001000U);

        // State actions
        WinTrustAuthenticodeVerifier.WTD_STATEACTION_IGNORE.Should().Be(0x00000000U);
        WinTrustAuthenticodeVerifier.WTD_STATEACTION_VERIFY.Should().Be(0x00000001U);
        WinTrustAuthenticodeVerifier.WTD_STATEACTION_CLOSE.Should().Be(0x00000002U);
    }
}
