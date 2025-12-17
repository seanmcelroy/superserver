namespace SuperServer.Configuration;

public class ServerConfiguration
{
    public DiscardConfiguration Discard { get; set; } = new();
    public EchoConfiguration Echo { get; set; } = new();
    public CharGenConfiguration CharGen { get; set; } = new();
    public DaytimeConfiguration Daytime { get; set; } = new();
}
