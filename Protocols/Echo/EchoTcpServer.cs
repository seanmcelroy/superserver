using System.Buffers;

namespace SuperServer.Protocols.Echo;

public class EchoTcpServer : TcpServerBase
{
    public override string ProtocolName => "echo";

    public int BufferLength { get; init; } = 1024;

    protected override async Task HandleClientAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BufferLength);
        try
        {
            int length;
            while (!cancellationToken.IsCancellationRequested && (length = await stream.ReadAsync(buffer.AsMemory(0, BufferLength), cancellationToken)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, length), cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
