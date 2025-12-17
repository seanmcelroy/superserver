using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperServer.Configuration;
using SuperServer.Protocols.Echo;

namespace SuperServer.Services;

public class EchoService : BackgroundService
{
    private readonly ILogger<EchoService> _logger;
    private readonly IOptionsMonitor<ServerConfiguration> _optionsMonitor;
    private readonly IDisposable? _optionsChangeToken;
    private EchoTcpServer? _tcpServer;
    private EchoUdpServer? _udpServer;

    // Store initial config for detecting restart-required changes
    private readonly string _initialListenAddress;
    private readonly ushort _initialTcpPort;
    private readonly ushort _initialUdpPort;

    public EchoService(ILogger<EchoService> logger, IOptionsMonitor<ServerConfiguration> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;

        var config = _optionsMonitor.CurrentValue.Echo;
        _initialListenAddress = config.ListenAddress;
        _initialTcpPort = config.TcpPort;
        _initialUdpPort = config.UdpPort;

        _optionsChangeToken = _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(ServerConfiguration config, string? name)
    {
        var echoConfig = config.Echo;

        // Check for changes that require restart
        if (echoConfig.ListenAddress != _initialListenAddress ||
            echoConfig.TcpPort != _initialTcpPort ||
            echoConfig.UdpPort != _initialUdpPort)
        {
            _logger.LogWarning(
                "Echo service configuration changed (address/port). Restart required to apply: " +
                "ListenAddress={NewAddress} (was {OldAddress}), TcpPort={NewTcpPort} (was {OldTcpPort}), " +
                "UdpPort={NewUdpPort} (was {OldUdpPort})",
                echoConfig.ListenAddress, _initialListenAddress,
                echoConfig.TcpPort, _initialTcpPort,
                echoConfig.UdpPort, _initialUdpPort);
        }

        // Log rate limit changes that are applied immediately
        _logger.LogInformation(
            "Echo service configuration reloaded. Rate limits: {MaxRps} req/s, {Window}s window",
            echoConfig.UdpMaxRequestsPerSecond?.ToString() ?? "unlimited",
            echoConfig.UdpRateLimitWindowSeconds);
    }

    private EchoConfiguration CurrentConfig => _optionsMonitor.CurrentValue.Echo;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = CurrentConfig;

        if (!config.Enabled)
        {
            _logger.LogInformation("Echo service is disabled");
            return;
        }

        if (!config.TcpEnabled && !config.UdpEnabled)
        {
            _logger.LogInformation("Echo service has both TCP and UDP disabled");
            return;
        }

        if (!IPAddress.TryParse(config.ListenAddress, out var listenAddress))
        {
            _logger.LogError("Invalid listen address '{Address}' for Echo service", config.ListenAddress);
            return;
        }

        var tasks = new List<Task>();

        if (config.TcpEnabled)
        {
            _tcpServer = new EchoTcpServer
            {
                ListenAddress = listenAddress,
                ListenPort = config.TcpPort,
                TcpMaxConnections = config.TcpMaxConnections,
                IdleTimeoutSeconds = config.TcpIdleTimeoutSeconds,
                Logger = _logger
            };
            tasks.Add(_tcpServer.Start(stoppingToken));
            _logger.LogInformation("Echo TCP server starting on {Address}:{Port}", config.ListenAddress, config.TcpPort);
        }

        if (config.UdpEnabled)
        {
            _udpServer = new EchoUdpServer
            {
                ListenAddress = listenAddress,
                ListenPort = config.UdpPort,
                ConfigurationProvider = () => (CurrentConfig.UdpMaxRequestsPerSecond, CurrentConfig.UdpRateLimitWindowSeconds),
                Logger = _logger
            };
            tasks.Add(_udpServer.Start(stoppingToken));
            _logger.LogInformation("Echo UDP server starting on {Address}:{Port}", config.ListenAddress, config.UdpPort);
        }
        else
        {
            _logger.LogDebug("Echo UDP server disabled (default for security - reflection attack mitigation)");
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            _tcpServer?.Stop();
            _udpServer?.Stop();
            _logger.LogInformation("Echo service stopped");
        }
    }

    public override void Dispose()
    {
        _optionsChangeToken?.Dispose();
        _tcpServer?.Dispose();
        _udpServer?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
