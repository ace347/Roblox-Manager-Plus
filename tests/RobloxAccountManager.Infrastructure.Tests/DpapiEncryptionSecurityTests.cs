using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using RobloxAccountManager.Infrastructure.Security;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests;

public class DpapiEncryptionSecurityTests
{
    private readonly DpapiEncryptionService _service = new();

    [Fact]
    public void Encrypt_EmptyPlaintext_ReturnsEmptyArray()
    {
        var result = _service.Encrypt(Array.Empty<byte>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_EmptyCiphertext_ReturnsEmptyArray()
    {
        var result = _service.Decrypt(Array.Empty<byte>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void EncryptAndDecrypt_WithCustomEntropy_Succeeds()
    {
        var plaintext = Encoding.UTF8.GetBytes("SuperSecretToken_Mock");
        var entropy = Encoding.UTF8.GetBytes("RandomPerAccountSalt");

        var encrypted = _service.Encrypt(plaintext, entropy);
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotEqual(plaintext);

        var decrypted = _service.Decrypt(encrypted, entropy);
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void Decrypt_WithWrongEntropy_ThrowsCryptographicException()
    {
        var plaintext = Encoding.UTF8.GetBytes("ConfidentialSecret");
        var validEntropy = Encoding.UTF8.GetBytes("ValidSalt");
        var invalidEntropy = Encoding.UTF8.GetBytes("WrongSalt");

        var encrypted = _service.Encrypt(plaintext, validEntropy);

        var act = () => _service.Decrypt(encrypted, invalidEntropy);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_WithCorruptedCiphertext_ThrowsCryptographicException()
    {
        var plaintext = Encoding.UTF8.GetBytes("TestMessage");
        var encrypted = _service.Encrypt(plaintext);

        // Flip bits in the middle of ciphertext
        encrypted[encrypted.Length / 2] ^= 0xFF;

        var act = () => _service.Decrypt(encrypted);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Encrypt_LargePayload_RoundTripsSuccessfully()
    {
        // 1 MB test payload
        var largePayload = new byte[1024 * 1024];
        RandomNumberGenerator.Fill(largePayload);

        var encrypted = _service.Encrypt(largePayload);
        var decrypted = _service.Decrypt(encrypted);

        decrypted.Should().Equal(largePayload);
    }
}
