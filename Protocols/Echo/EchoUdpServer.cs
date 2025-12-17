using System.Net.Sockets;

namespace SuperServer.Protocols.Echo;

public class EchoUdpServer : UdpServerBase
{
    public override string ProtocolName => "echo";

    protected override async Task ProcessLoop(UdpClient client, CancellationToken cancellationToken)
    {
        try
        {
            do
            {
                var recv = await client.ReceiveAsync(cancellationToken);
                TrackBytesReceived(recv.Buffer.Length);
                if (IsRateLimited(recv.RemoteEndPoint.Address))
                    continue;
                TrackRequest();
                await client.SendAsync(recv.Buffer, recv.RemoteEndPoint, cancellationToken);
                TrackBytesSent(recv.Buffer.Length);
            }
            while (!cancellationToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { } // Client disconnected unexpectedly
    }
}