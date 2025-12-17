namespace SuperServer.Configuration;

public class DaytimeConfiguration : ProtocolConfiguration
{
    public DaytimeConfiguration()
    {
        TcpPort = 2013;
        UdpPort = 2013;
    }
}
