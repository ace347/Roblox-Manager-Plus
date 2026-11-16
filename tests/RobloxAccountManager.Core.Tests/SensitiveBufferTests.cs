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
        using var buffer = new SensitiveBuffer(source);
        var retainedBuffer = buffer.DangerousGetBuffer();

        buffer.Length.Should().Be(5);
        retainedBuffer.Should().Equal(source);

        buffer.Dispose();
        retainedBuffer.Should().OnlyContain(value => value == 0);

        // Access after disposal should fail safely
        var act = () => buffer.DangerousGetBuffer();
        act.Should().Throw<ObjectDisposedException>();
    }
}
