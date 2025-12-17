using System.Net.Sockets;

namespace SuperServer.Protocols.Discard;

public class DiscardUdpServer : UdpServerBase
{
    public override string ProtocolName => "discard";

    protected override async Task ProcessLoop(UdpClient client, CancellationToken cancellationToken)
    {
        try
        {
            do
            {
                var recv = await client.ReceiveAsync(cancellationToken);
                TrackBytesReceived(recv.Buffer.Length);
                // Rate limit check still useful to prevent excessive CPU usage
                if (IsRateLimited(recv.RemoteEndPoint.Address))
                    continue;
                TrackRequest();
                // Discard protocol: data is intentionally ignored
            }
            while (!cancellationToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { } // Client disconnected unexpectedly
    }
}