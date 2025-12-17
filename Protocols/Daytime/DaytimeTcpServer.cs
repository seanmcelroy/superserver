using System.Buffers;
using System.Globalization;

namespace SuperServer.Protocols.Daytime;

public class DaytimeTcpServer : TcpServerBase
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

    protected override async Task HandleClientAsync(Stream stream, CancellationToken cancellationToken)
    {
        stream.WriteTimeout = 30000; // 30 seconds until I timeout if the client can't acknowledge.
        if (!cancellationToken.IsCancellationRequested && stream.CanWrite)
        {
            var (buffer, length) = DaytimeServerBase.FormatDaytimePooled(_culture!, FormatSpecifier);
            try
            {
                await stream.WriteAsync(buffer.AsMemory(0, length), cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
