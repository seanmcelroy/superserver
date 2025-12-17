using System.Net.Sockets;

namespace SuperServer.Metrics;

/// <summary>
/// A NetworkStream wrapper that tracks bytes read and written for metrics.
/// </summary>
public class MeteredStream : Stream
{
    private readonly NetworkStream _innerStream;
    private readonly string _protocol;
    private readonly string _transport;

    public MeteredStream(NetworkStream innerStream, string protocol, string transport)
    {
        _innerStream = innerStream;
        _protocol = protocol;
        _transport = transport;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override bool CanTimeout => _innerStream.CanTimeout;
    public override int ReadTimeout
    {
        get => _innerStream.ReadTimeout;
        set => _innerStream.ReadTimeout = value;
    }
    public override int WriteTimeout
    {
        get => _innerStream.WriteTimeout;
        set => _innerStream.WriteTimeout = value;
    }

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override void Flush() => _innerStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        if (bytesRead > 0)
        {
            ServerMetrics.BytesReceivedTotal.WithLabels(_protocol, _transport).Inc(bytesRead);
        }
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        if (bytesRead > 0)
        {
            ServerMetrics.BytesReceivedTotal.WithLabels(_protocol, _transport).Inc(bytesRead);
        }
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);
        if (bytesRead > 0)
        {
            ServerMetrics.BytesReceivedTotal.WithLabels(_protocol, _transport).Inc(bytesRead);
        }
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
        ServerMetrics.BytesSentTotal.WithLabels(_protocol, _transport).Inc(count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        ServerMetrics.BytesSentTotal.WithLabels(_protocol, _transport).Inc(count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _innerStream.WriteAsync(buffer, cancellationToken);
        ServerMetrics.BytesSentTotal.WithLabels(_protocol, _transport).Inc(buffer.Length);
    }

    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => _innerStream.SetLength(value);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
