namespace SuperServer.Configuration;

public class HealthCheckConfiguration
{
    public bool Enabled { get; set; } = true;
    public string ListenAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;
}
