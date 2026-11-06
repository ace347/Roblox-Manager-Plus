namespace RobloxAccountManager.Core.Models;

/// <summary>
/// Status codes returned by the Authenticode signature verification subsystem.
/// </summary>
public enum AuthenticodeStatus
{
    /// <summary>File does not exist or could not be opened.</summary>
    FileNotFound,

    /// <summary>The file contains no embedded or catalog Authenticode signature.</summary>
    NotSigned,

    /// <summary>The file has been modified or corrupted after signing (hash/digest mismatch).</summary>
    CorruptedOrModified,

    /// <summary>The certificate chains to an untrusted root certification authority.</summary>
    UntrustedRoot,

    /// <summary>The certificate or a CA in the chain has been explicitly revoked.</summary>
    Revoked,

    /// <summary>The signing certificate or timestamp has expired without a valid counter-signature.</summary>
    Expired,

    /// <summary>The certificate or publisher was explicitly marked as distrusted on this system.</summary>
    ExplicitlyDistrusted,

    /// <summary>Revocation status could not be verified because revocation server/cache was offline or unavailable.</summary>
    RevocationOfflineOrUnavailable,

    /// <summary>A certificate chain could not be built to a trusted root authority.</summary>
    ChainingError,

    /// <summary>The signature is cryptographically valid, but the publisher does not match expected identity.</summary>
    PublisherMismatch,

    /// <summary>A generic or unrecognized trust provider verification error occurred.</summary>
    VerificationFailed,

    /// <summary>The signature is valid, trusted, uncorrupted, and published by the expected entity.</summary>
    Valid
}

/// <summary>
/// Result of an Authenticode signature verification operation.
/// </summary>
public sealed class AuthenticodeResult
{
    /// <summary>Whether the binary is considered valid and safe to trust.</summary>
    public bool IsValid => Status == AuthenticodeStatus.Valid;

    /// <summary>Detailed verification status code.</summary>
    public AuthenticodeStatus Status { get; init; }

    /// <summary>Signer certificate subject name (distinguished name), or empty if unavailable.</summary>
    public string SignerSubject { get; init; } = string.Empty;

    /// <summary>Raw HRESULT returned by WinVerifyTrust.</summary>
    public int RawHResult { get; init; }

    /// <summary>Human-readable diagnostic message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Creates a successful verification result.</summary>
    public static AuthenticodeResult Success(string subject, int hresult = 0) =>
        new()
        {
            Status = AuthenticodeStatus.Valid,
            SignerSubject = subject,
            RawHResult = hresult,
            Message = "Authenticode signature verified successfully and matches expected publisher."
        };

    /// <summary>Creates a failed verification result.</summary>
    public static AuthenticodeResult Failure(AuthenticodeStatus status, int hresult, string message, string subject = "") =>
        new()
        {
            Status = status,
            SignerSubject = subject,
            RawHResult = hresult,
            Message = message
        };
}

