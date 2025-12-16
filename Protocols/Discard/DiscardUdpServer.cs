using System.Net.Sockets;

namespace SuperServer.Protocols.Discard;

public class DiscardUdpServer : UdpServerBase
{
    public int BufferLength { get; init; } = 1024;

    protected override async Task ProcessLoop(UdpClient client, CancellationToken cancellationToken)
    {
        try
        {
            do
            {
                // Processing
                _ = await client.ReceiveAsync(cancellationToken);
            }
            while (!cancellationToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { } // Client disconnected unexpectedly
    }
}