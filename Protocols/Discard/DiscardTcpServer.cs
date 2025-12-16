using System.Net.Sockets;

namespace SuperServer.Protocols.Discard;

public class DiscardTcpServer : TcpServerBase
{
    public int BufferLength { get; init; } = 1024;

    protected override async Task HandleClientAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferLength];
        while (!cancellationToken.IsCancellationRequested && (_ = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            // Do nothing, discard.
        }
    }
}