namespace SuperServer.Configuration;

public abstract class ProtocolConfiguration
{
    /// <summary>
    /// Master enable/disable for this protocol. When false, neither TCP nor UDP servers are started.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enable TCP server for this protocol. Defaults to true.
    /// </summary>
    public bool TcpEnabled { get; set; } = true;

    /// <summary>
    /// Enable UDP server for this protocol. Defaults to true, but may be overridden
    /// in protocol-specific configurations for security reasons.
    /// </summary>
    public virtual bool UdpEnabled { get; set; } = true;

    public ushort TcpPort { get; set; }
    public ushort UdpPort { get; set; }
    public string ListenAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Maximum concurrent TCP connections. Set to null for unlimited (not recommended).
    /// </summary>
    public int? TcpMaxConnections { get; set; } = 100;

    /// <summary>
    /// Idle timeout in seconds for TCP connections. Connections that don't receive data
    /// within this period will be closed. Set to null for no timeout (not recommended).
    /// </summary>
    public int? TcpIdleTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum UDP requests per second per source IP. Set to null for unlimited (not recommended).
    /// </summary>
    public int? UdpMaxRequestsPerSecond { get; set; } = 100;

    /// <summary>
    /// Time window in seconds for UDP rate limiting. Requests are counted within this sliding window.
    /// </summary>
    public int UdpRateLimitWindowSeconds { get; set; } = 1;
}
