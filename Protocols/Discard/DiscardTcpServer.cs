using System.Buffers;

namespace SuperServer.Protocols.Discard;

public class DiscardTcpServer : TcpServerBase
{
    public override string ProtocolName => "discard";

    public int BufferLength { get; init; } = 1024;

    protected override async Task HandleClientAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BufferLength);
        try
        {
            while (!cancellationToken.IsCancellationRequested && (_ = await stream.ReadAsync(buffer.AsMemory(0, BufferLength), cancellationToken)) > 0)
            {
                // Do nothing, discard.
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
