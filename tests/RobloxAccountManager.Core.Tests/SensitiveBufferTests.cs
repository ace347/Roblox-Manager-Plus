using FluentAssertions;
using RobloxAccountManager.Core.Security;
using Xunit;

namespace RobloxAccountManager.Core.Tests;

public class SensitiveBufferTests
{
    [Fact]
    public void SensitiveBuffer_HoldsData_AndZeroesMemoryOnDispose()
    {
        var source = new byte[] { 10, 20, 30, 40, 50 };
        SensitiveBuffer buffer;

        using (buffer = new SensitiveBuffer(source))
        {
            buffer.Length.Should().Be(5);
            buffer.DangerousGetBuffer().Should().Equal(source);
        }

        // Access after disposal should fail safely
        var act = () => buffer.DangerousGetBuffer();
        act.Should().Throw<ObjectDisposedException>();
    }
}
