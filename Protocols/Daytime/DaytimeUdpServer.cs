using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace SuperServer.Protocols.Daytime;

public class DaytimeUdpServer : UdpServerBase
{
    public string Culture { get; init; } = "en-US";
    public string FormatSpecifier { get; init; } = "o";

    private CultureInfo? _culture;

    public override async Task Start(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Culture))
            _culture = CultureInfo.InvariantCulture;
        else
        {
            try
            {
                _culture = CultureInfo.GetCultureInfo(Culture);
            }
            catch (CultureNotFoundException)
            {
                _culture = CultureInfo.InvariantCulture;
            }
        }

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

                // Create daytime datagram (recv contents are discarded)
                byte[] line;
                try
                {
                    line = Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString(FormatSpecifier, _culture) + "\r\n");
                }
                catch (FormatException)
                {
                    line = Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString("o", _culture) + "\r\n");
                }

                // Send generated characters
                await client.SendAsync(line, recv.RemoteEndPoint, cancellationToken);
            } while (!cancellationToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { } // Client disconnected unexpectedly
    }
}