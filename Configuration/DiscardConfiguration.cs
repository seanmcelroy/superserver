namespace SuperServer.Configuration;

public class DiscardConfiguration : ProtocolConfiguration
{
    public DiscardConfiguration()
    {
        TcpPort = 2009;
        UdpPort = 2009;
    }
}
