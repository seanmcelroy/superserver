using System.Net;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("superserver");

        using var echoTcp = new SuperServer.Protocols.Echo.EchoTcpServer
        {
            ListenAddress = IPAddress.Loopback,
            ListenPort = 2007
        };
        using var echoUdp = new SuperServer.Protocols.Echo.EchoUdpServer
        {
            ListenAddress = IPAddress.Loopback,
            ListenPort = 2007
        };
        using var discardTcp = new SuperServer.Protocols.Discard.DiscardTcpServer
        {
            ListenAddress = IPAddress.Loopback,
            ListenPort = 2009
        };
        using var discardUdp = new SuperServer.Protocols.Discard.DiscardUdpServer
        {
            ListenAddress = IPAddress.Loopback,
            ListenPort = 2009
        };
        using var daytimeTcp = new SuperServer.Protocols.Daytime.DaytimeTcpServer
        {
            ListenAddress = IPAddress.Loopback,
            ListenPort = 2013
        };
        using var daytimeUdp = new SuperServer.Protocols.Daytime.DaytimeUdpServer
        {
            ListenAddress = IPAddress.Loopback,
            ListenPort = 2013
        };
        using var chargenTcp = new SuperServer.Protocols.CharacterGenerator.CharGenTcpServer
        {
            ListenAddress = IPAddress.Loopback,
            ListenPort = 2019
        };
        using var chargenUdp = new SuperServer.Protocols.CharacterGenerator.CharGenUdpServer
        {
            ListenAddress = IPAddress.Loopback,
            ListenPort = 2019
        };

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += new ConsoleCancelEventHandler((o,e) =>
        {
            cts.Cancel();
        });

        try
        {
            await Task.WhenAll(
                echoTcp.Start(cts.Token),
                echoUdp.Start(cts.Token),
                discardTcp.Start(cts.Token),
                discardUdp.Start(cts.Token),
                daytimeTcp.Start(cts.Token),
                daytimeUdp.Start(cts.Token),
                chargenTcp.Start(cts.Token),
                chargenUdp.Start(cts.Token)
            );
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        echoTcp.Stop();
        echoUdp.Stop();
        discardTcp.Stop();
        discardUdp.Stop();
        daytimeTcp.Stop();
        daytimeUdp.Stop();
        chargenTcp.Stop();
        chargenUdp.Stop();
    }

    /*private static byte[] BuildHeader(ushort id, bool isQuery)
    {
        var headerBytes = new byte[2 * 6];
        var headerSpan = new Span<byte>(headerBytes);
        id.TryFormat(headerSpan[..2], out int idBytesWritten);
        BitConverter.

        isQuery.TryFormat(headerSpan[16..17], out int qrBytesWritten)

        return headerBytes;
    }*/
}