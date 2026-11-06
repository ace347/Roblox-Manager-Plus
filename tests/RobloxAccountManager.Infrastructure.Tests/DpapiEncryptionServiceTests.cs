using System.Text;
using FluentAssertions;
using RobloxAccountManager.Infrastructure.Security;
using Xunit;

namespace RobloxAccountManager.Infrastructure.Tests;

public class DpapiEncryptionServiceTests
{
    [Fact]
    public void EncryptAndDecrypt_RoundTrip_ReturnsOriginalPlaintext()
    {
        var service = new DpapiEncryptionService();
        var originalText = "TestSensitiveSecret123!";
        var originalBytes = Encoding.UTF8.GetBytes(originalText);
        var entropy = Encoding.UTF8.GetBytes("ApplicationSalt");

        var encrypted = service.Encrypt(originalBytes, entropy);
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotEqual(originalBytes);

        var decrypted = service.Decrypt(encrypted, entropy);
        var decryptedText = Encoding.UTF8.GetString(decrypted);

        decryptedText.Should().Be(originalText);
    }
}
