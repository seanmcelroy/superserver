namespace SuperServer.Configuration;

public class EchoConfiguration : ProtocolConfiguration
{
    /// <summary>
    /// UDP is disabled by default for Echo due to reflection/amplification attack risk.
    /// Enable explicitly if needed, but be aware of the security implications.
    /// </summary>
    public override bool UdpEnabled { get; set; } = false;

    public EchoConfiguration()
    {
        TcpPort = 2007;
        UdpPort = 2007;
    }
}
