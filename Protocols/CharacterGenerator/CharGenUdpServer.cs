using System.Net.Sockets;

namespace SuperServer.Protocols.CharacterGenerator;

public class CharGenUdpServer : UdpServerBase
{
    public override async Task Start(CancellationToken cancellationToken)
    {
        Console.Error.WriteLine("WARNING: Security Note - CHARGEN over UDP is a known amplification attack vector - a small request can trigger a larger response (up to 512x amplification). This is inherent to the protocol, not a bug. CHARGEN is typically disabled on production systems for this reason. Fine for testing/educational purposes.");
        await base.Start(cancellationToken);
    }

    protected override async Task ProcessLoop(UdpClient client, CancellationToken cancellationToken)
    {
        try
        {
            do
            {
                // Processing
                var recv = await client.ReceiveAsync(cancellationToken);

                // Create random datagram (recv contents are discarded)
                var charCount = Random.Shared.Next(0, 513);
                var chars = new byte[charCount];
                var n = Random.Shared.Next(0, 95);
                for (var c = 0; c < charCount; c++)
                    chars[c] = (byte)(((c + n) % 95) + 32);

                // Send generated characters
                await client.SendAsync(chars, recv.RemoteEndPoint, cancellationToken);
            }
            while (!cancellationToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { } // Client disconnected unexpectedly
    }
}