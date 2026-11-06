using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Core.Models;

namespace RobloxAccountManager.Infrastructure.RobloxIntegration;

/// <summary>
/// Verifies Authenticode digital signatures on Windows Portable Executables (PE) using Win32 WinVerifyTrust.
/// Performs comprehensive checks including:
/// - Cryptographic digest and file integrity (detects tamper/corruption)
/// - Certificate trust chain validation to a trusted Root Certification Authority
/// - Revocation checking policy enforcement
/// - Expiration and timestamp token validation
/// - Signer certificate subject identity matching
/// </summary>
public sealed class WinTrustAuthenticodeVerifier : IAuthenticodeVerifier
{
    private static readonly Guid ActionGenericVerifyV2 = new("00aac56b-cd44-11d0-8cc2-00c04fc295ee");

    // WinVerifyTrust and CryptoAPI HRESULT error constants
    private const int S_OK = 0x00000000;
    private const int TRUST_E_NOSIGNATURE = unchecked((int)0x800B0100);
    private const int TRUST_E_BAD_DIGEST = unchecked((int)0x80096010);
    private const int TRUST_E_SUBJECT_FORM_UNKNOWN = unchecked((int)0x800B0003);
    private const int TRUST_E_PROVIDER_UNKNOWN = unchecked((int)0x800B0001);
    private const int TRUST_E_ACTION_UNKNOWN = unchecked((int)0x800B0002);
    private const int TRUST_E_SUBJECT_NOT_TRUSTED = unchecked((int)0x800B0004);
    private const int TRUST_E_EXPLICIT_DISTRUST = unchecked((int)0x800B0111);
    private const int TRUST_E_TIME_STAMP = unchecked((int)0x80096005);
    private const int CERT_E_UNTRUSTEDROOT = unchecked((int)0x800B0109);
    private const int CERT_E_CHAINING = unchecked((int)0x800B010B);
    private const int CERT_E_REVOKED = unchecked((int)0x800B010C);
    private const int CERT_E_EXPIRED = unchecked((int)0x800B0101);
    private const int CERT_E_REVOCATION_FAILURE = unchecked((int)0x800B010E);
    private const int CRYPT_E_REVOCATION_OFFLINE = unchecked((int)0x80092013);
    private const int NTE_BAD_SIGNATURE = unchecked((int)0x80090006);

    private readonly ILogger<WinTrustAuthenticodeVerifier> _logger;

    public WinTrustAuthenticodeVerifier(ILogger<WinTrustAuthenticodeVerifier> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public AuthenticodeResult VerifyFile(string filePath, IEnumerable<string>? expectedPublisherSubstrings = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return AuthenticodeResult.Failure(
                AuthenticodeStatus.FileNotFound,
                -1,
                "File path is null or empty.");
        }

        if (!File.Exists(filePath))
        {
            return AuthenticodeResult.Failure(
                AuthenticodeStatus.FileNotFound,
                -1,
                $"File does not exist: {filePath}");
        }

        // Step 1: Execute WinVerifyTrust via P/Invoke
        int hresult = ExecuteWinVerifyTrust(filePath);

        // Step 2: Extract Signer Certificate Subject if an Authenticode block exists
        string signerSubject = ExtractSignerSubject(filePath);

        // Step 3: Map WinVerifyTrust HRESULT to AuthenticodeStatus
        switch (hresult)
        {
            case S_OK:
                break; // Continue to publisher check

            case TRUST_E_NOSIGNATURE:
            case TRUST_E_SUBJECT_FORM_UNKNOWN:
                _logger.LogWarning("Binary is not signed: {Path} (0x{HResult:X8})", filePath, hresult);
                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.NotSigned,
                    hresult,
                    "No Authenticode signature found on file.",
                    signerSubject);

            case TRUST_E_BAD_DIGEST:
            case NTE_BAD_SIGNATURE:
                _logger.LogError("Binary signature integrity failure / modified: {Path} (0x{HResult:X8})", filePath, hresult);
                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.CorruptedOrModified,
                    hresult,
                    "The file has been modified or corrupted after signing (bad digest).",
                    signerSubject);

            case CERT_E_UNTRUSTEDROOT:
                _logger.LogError("Binary certificate chains to untrusted root: {Path} (0x{HResult:X8})", filePath, hresult);
                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.UntrustedRoot,
                    hresult,
                    "Certificate chain terminates in an untrusted root authority.",
                    signerSubject);

            case CERT_E_CHAINING:
            case TRUST_E_SUBJECT_NOT_TRUSTED:
                _logger.LogError("Certificate chaining error: {Path} (0x{HResult:X8})", filePath, hresult);
                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.ChainingError,
                    hresult,
                    "Certificate chain could not be built to a trusted root authority.",
                    signerSubject);

            case CERT_E_REVOKED:
                _logger.LogError("Certificate revoked for binary: {Path} (0x{HResult:X8})", filePath, hresult);
                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.Revoked,
                    hresult,
                    "The certificate used to sign the file has been explicitly revoked.",
                    signerSubject);

            case CERT_E_EXPIRED:
            case TRUST_E_TIME_STAMP:
                _logger.LogWarning("Certificate expired or invalid timestamp: {Path} (0x{HResult:X8})", filePath, hresult);
                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.Expired,
                    hresult,
                    "The certificate has expired or its timestamp is invalid.",
                    signerSubject);

            case TRUST_E_EXPLICIT_DISTRUST:
                _logger.LogError("Certificate explicitly distrusted by policy: {Path} (0x{HResult:X8})", filePath, hresult);
                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.ExplicitlyDistrusted,
                    hresult,
                    "The certificate or publisher was explicitly marked as distrusted on this system.",
                    signerSubject);

            case CERT_E_REVOCATION_FAILURE:
            case CRYPT_E_REVOCATION_OFFLINE:
                _logger.LogWarning("Revocation server offline for binary: {Path} (0x{HResult:X8})", filePath, hresult);
                // When revocation server is unreachable, we report verification failure unless policy specifies otherwise
                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.VerificationFailed,
                    hresult,
                    "Revocation status could not be verified because revocation server is offline.",
                    signerSubject);

            default:
                _logger.LogError("WinVerifyTrust failure: {Path} (0x{HResult:X8})", filePath, hresult);
                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.VerificationFailed,
                    hresult,
                    $"WinVerifyTrust failed with HRESULT 0x{hresult:X8}",
                    signerSubject);
        }

        // Step 4: Validate Publisher Identity against expected substrings
        if (expectedPublisherSubstrings is not null)
        {
            var matchFound = false;
            foreach (var expected in expectedPublisherSubstrings)
            {
                if (!string.IsNullOrEmpty(expected) &&
                    signerSubject.Contains(expected, StringComparison.OrdinalIgnoreCase))
                {
                    matchFound = true;
                    break;
                }
            }

            if (!matchFound)
            {
                _logger.LogWarning(
                    "Publisher identity mismatch for {Path}. Signer: '{Signer}'.",
                    filePath, signerSubject);

                return AuthenticodeResult.Failure(
                    AuthenticodeStatus.PublisherMismatch,
                    hresult,
                    $"Signature is valid, but publisher '{signerSubject}' did not match expected identity.",
                    signerSubject);
            }
        }

        _logger.LogInformation(
            "Authenticode signature valid and verified for {Path}. Signer: '{Signer}'",
            filePath, signerSubject);

        return AuthenticodeResult.Success(signerSubject, hresult);
    }

    private static int ExecuteWinVerifyTrust(string filePath)
    {
        var fileInfo = new WINTRUST_FILE_INFO();
        fileInfo.cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>();
        fileInfo.pcwszFilePath = Marshal.StringToHGlobalUni(filePath);
        fileInfo.hFile = IntPtr.Zero;
        fileInfo.pgKnownSubject = IntPtr.Zero;

        IntPtr pFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());
        Marshal.StructureToPtr(fileInfo, pFileInfo, false);

        var trustData = new WINTRUST_DATA();
        trustData.cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>();
        trustData.pPolicyCallbackData = IntPtr.Zero;
        trustData.pSIPClientData = IntPtr.Zero;
        trustData.dwUIChoice = 2; // WTD_UI_NONE - no dialogs or prompts
        trustData.fdwRevocationChecks = 0; // WTD_REVOKE_NONE (revocation governed by dwProvFlags)
        trustData.dwUnionChoice = 1; // WTD_CHOICE_FILE
        trustData.pFile = pFileInfo;
        trustData.dwStateAction = 0; // WTD_STATEACTION_IGNORE
        trustData.hWVTStateData = IntPtr.Zero;
        trustData.pwszURLReference = IntPtr.Zero;

        // WTD_REVOCATION_CHECK_CHAIN (0x00000040) enforces revocation checking across chain.
        // WTD_CACHE_ONLY_URL_RETRIEVAL (0x00001000) restricts lookup to local cache avoiding network hangs.
        trustData.dwProvFlags = 0x00000040 | 0x00001000;
        trustData.dwUIContext = 0;

        IntPtr pTrustData = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_DATA>());
        Marshal.StructureToPtr(trustData, pTrustData, false);

        try
        {
            return WinVerifyTrust(new IntPtr(-1), ActionGenericVerifyV2, pTrustData);
        }
        finally
        {
            Marshal.FreeHGlobal(fileInfo.pcwszFilePath);
            Marshal.FreeHGlobal(pFileInfo);
            Marshal.FreeHGlobal(pTrustData);
        }
    }

    private static string ExtractSignerSubject(string filePath)
    {
        try
        {
            if (X509Certificate2.GetCertContentType(filePath) == X509ContentType.Authenticode)
            {
#pragma warning disable SYSLIB0057 // Legacy method required for PE Authenticode extraction in .NET
                using var legacyCert = X509Certificate.CreateFromSignedFile(filePath);
#pragma warning restore SYSLIB0057
                using var cert2 = new X509Certificate2(legacyCert);
                return cert2.Subject;
            }
        }
        catch
        {
            // Best effort; WinVerifyTrust handles cryptographic validity
        }

        return string.Empty;
    }

    #region Win32 P/Invoke Declarations

    [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
    private static extern int WinVerifyTrust(
        IntPtr hwnd,
        [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
        IntPtr pWVTData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        public IntPtr pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
    }

    #endregion
}
