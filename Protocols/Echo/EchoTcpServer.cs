using System.Net.Sockets;

namespace SuperServer.Protocols.Echo;

public class EchoTcpServer : TcpServerBase
{
    public int BufferLength { get; init; } = 1024;

    protected override async Task HandleClientAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferLength];
        int length;
        while (!cancellationToken.IsCancellationRequested && (length = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await stream.WriteAsync(buffer.AsMemory(0, length), cancellationToken);
        }
    }
}