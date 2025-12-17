namespace SuperServer.Configuration;

public class CharGenConfiguration : ProtocolConfiguration
{
    /// <summary>
    /// UDP is disabled by default for CharGen due to severe amplification attack risk.
    /// A small request can trigger up to 512x response amplification.
    /// Enable explicitly only if you understand the security implications.
    /// </summary>
    public override bool UdpEnabled { get; set; } = false;

    public CharGenConfiguration()
    {
        TcpPort = 2019;
        UdpPort = 2019;
    }
}
