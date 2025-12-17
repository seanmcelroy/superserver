using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace SuperServer.Protocols.CharacterGenerator;

public class CharGenUdpServer : UdpServerBase
{
    public override string ProtocolName => "chargen";

    public override async Task Start(CancellationToken cancellationToken)
    {
        Logger?.LogWarning("Security Note - CHARGEN over UDP is a known amplification attack vector. " +
            "A small request can trigger a larger response (up to 512x amplification). " +
            "This is inherent to the protocol, not a bug. " +
            "CHARGEN is typically disabled on production systems for this reason.");
        await base.Start(cancellationToken);
    }

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

                // Create random datagram (recv contents are discarded)
                var charCount = Random.Shared.Next(0, 513);
                var chars = new byte[charCount];
                var n = Random.Shared.Next(0, 95);
                for (var c = 0; c < charCount; c++)
                    chars[c] = (byte)(((c + n) % 95) + 32);

                await client.SendAsync(chars, recv.RemoteEndPoint, cancellationToken);
                TrackBytesSent(charCount);
            }
            while (!cancellationToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
    }
}
