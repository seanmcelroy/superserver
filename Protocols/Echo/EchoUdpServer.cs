using System.Net.Sockets;

namespace SuperServer.Protocols.Echo;

public class EchoUdpServer : UdpServerBase
{
    protected override async Task ProcessLoop(UdpClient client, CancellationToken cancellationToken)
    {
        try
        {
            do
            {
                // Processing
                var recv = await client.ReceiveAsync(cancellationToken);
                await client.SendAsync(recv.Buffer, recv.RemoteEndPoint, cancellationToken);
            }
            while (!cancellationToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { } // Client disconnected unexpectedly
    }
}