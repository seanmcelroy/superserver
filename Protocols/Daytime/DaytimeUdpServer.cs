using System.Buffers;
using System.Globalization;
using System.Net.Sockets;

namespace SuperServer.Protocols.Daytime;

public class DaytimeUdpServer : UdpServerBase
{
    public override string ProtocolName => "daytime";

    public string Culture { get; init; } = "en-US";
    public string FormatSpecifier { get; init; } = "o";

    private CultureInfo? _culture;

    public override async Task Start(CancellationToken cancellationToken)
    {
        _culture = DaytimeServerBase.ParseCulture(Culture);
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
                var (buffer, length) = DaytimeServerBase.FormatDaytimePooled(_culture!, FormatSpecifier);
                try
                {
                    await client.SendAsync(buffer.AsMemory(0, length), recv.RemoteEndPoint, cancellationToken);
                    TrackBytesSent(length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            } while (!cancellationToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
    }
}
