using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace SuperServer.Protocols.Daytime;

public class DaytimeTcpServer : TcpServerBase
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

    protected override async Task HandleClientAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        stream.WriteTimeout = 30000; // 30 seconds until I timeout if the client can't acknowledge.
        if (!cancellationToken.IsCancellationRequested && stream.CanWrite)
        {
            byte[] line;
            try
            {
                line = Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString(FormatSpecifier, _culture) + "\r\n");
            }
            catch (FormatException)
            {
                line = Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString("o", _culture) + "\r\n");
            }
            await stream.WriteAsync(line, cancellationToken);
        }
    }
}