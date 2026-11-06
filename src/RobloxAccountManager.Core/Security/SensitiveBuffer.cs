using System.Security.Cryptography;

namespace RobloxAccountManager.Core.Security;

/// <summary>
/// Safe managed wrapper for holding sensitive binary credentials in memory.
/// Disposes and zeros out the underlying byte buffer immediately when finished.
/// </summary>
public sealed class SensitiveBuffer : IDisposable
{
    private byte[]? _buffer;
    private bool _disposed;

    public SensitiveBuffer(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = (byte[])buffer.Clone();
    }

    public byte[] DangerousGetBuffer()
    {
        ObjectDisposedException.ThrowIf(_disposed || _buffer is null, this);
        return _buffer;
    }

    public int Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed || _buffer is null, this);
            return _buffer.Length;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_buffer != null)
            {
                CryptographicOperations.ZeroMemory(_buffer);
                _buffer = null;
            }
            _disposed = true;
        }
    }
}
