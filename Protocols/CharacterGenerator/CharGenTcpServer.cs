namespace SuperServer.Protocols.CharacterGenerator;

public class CharGenTcpServer : TcpServerBase
{
    public override string ProtocolName => "chargen";

    public ulong? MaximumLines { get; init; }

    protected override async Task HandleClientAsync(Stream stream, CancellationToken cancellationToken)
    {
        stream.WriteTimeout = 30000; // 30 seconds until I timeout if the client can't acknowledge.
        ulong n = 0;
        var line = new byte[74];
        line[72] = 13; // CR
        line[73] = 10; // LF
        while (!cancellationToken.IsCancellationRequested && stream.CanWrite && (MaximumLines == null || n < MaximumLines.Value))
        {
            for (ulong c = 0; c < 72; c++)
                line[c] = (byte)(((c + n) % 95) + 32);
            await stream.WriteAsync(line.AsMemory(0, 74), cancellationToken);
            n++;

            await Task.Delay(10, cancellationToken);
        }
    }
}