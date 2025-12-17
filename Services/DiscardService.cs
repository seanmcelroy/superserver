using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperServer.Configuration;
using SuperServer.Protocols.Discard;

namespace SuperServer.Services;

public class DiscardService : BackgroundService
{
    private readonly ILogger<DiscardService> _logger;
    private readonly IOptionsMonitor<ServerConfiguration> _optionsMonitor;
    private readonly IDisposable? _optionsChangeToken;
    private DiscardTcpServer? _tcpServer;
    private DiscardUdpServer? _udpServer;

    // Store initial config for detecting restart-required changes
    private readonly string _initialListenAddress;
    private readonly ushort _initialTcpPort;
    private readonly ushort _initialUdpPort;

    public DiscardService(ILogger<DiscardService> logger, IOptionsMonitor<ServerConfiguration> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;

        var config = _optionsMonitor.CurrentValue.Discard;
        _initialListenAddress = config.ListenAddress;
        _initialTcpPort = config.TcpPort;
        _initialUdpPort = config.UdpPort;

        _optionsChangeToken = _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(ServerConfiguration config, string? name)
    {
        var discardConfig = config.Discard;

        // Check for changes that require restart
        if (discardConfig.ListenAddress != _initialListenAddress ||
            discardConfig.TcpPort != _initialTcpPort ||
            discardConfig.UdpPort != _initialUdpPort)
        {
            _logger.LogWarning(
                "Discard service configuration changed (address/port). Restart required to apply: " +
                "ListenAddress={NewAddress} (was {OldAddress}), TcpPort={NewTcpPort} (was {OldTcpPort}), " +
                "UdpPort={NewUdpPort} (was {OldUdpPort})",
                discardConfig.ListenAddress, _initialListenAddress,
                discardConfig.TcpPort, _initialTcpPort,
                discardConfig.UdpPort, _initialUdpPort);
        }

        // Log rate limit changes that are applied immediately
        _logger.LogInformation(
            "Discard service configuration reloaded. Rate limits: {MaxRps} req/s, {Window}s window",
            discardConfig.UdpMaxRequestsPerSecond?.ToString() ?? "unlimited",
            discardConfig.UdpRateLimitWindowSeconds);
    }

    private DiscardConfiguration CurrentConfig => _optionsMonitor.CurrentValue.Discard;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = CurrentConfig;

        if (!config.Enabled)
        {
            _logger.LogInformation("Discard service is disabled");
            return;
        }

        if (!config.TcpEnabled && !config.UdpEnabled)
        {
            _logger.LogInformation("Discard service has both TCP and UDP disabled");
            return;
        }

        if (!IPAddress.TryParse(config.ListenAddress, out var listenAddress))
        {
            _logger.LogError("Invalid listen address '{Address}' for Discard service", config.ListenAddress);
            return;
        }

        var tasks = new List<Task>();

        if (config.TcpEnabled)
        {
            _tcpServer = new DiscardTcpServer
            {
                ListenAddress = listenAddress,
                ListenPort = config.TcpPort,
                TcpMaxConnections = config.TcpMaxConnections,
                IdleTimeoutSeconds = config.TcpIdleTimeoutSeconds,
                Logger = _logger
            };
            tasks.Add(_tcpServer.Start(stoppingToken));
            _logger.LogInformation("Discard TCP server starting on {Address}:{Port}", config.ListenAddress, config.TcpPort);
        }

        if (config.UdpEnabled)
        {
            _udpServer = new DiscardUdpServer
            {
                ListenAddress = listenAddress,
                ListenPort = config.UdpPort,
                ConfigurationProvider = () => (CurrentConfig.UdpMaxRequestsPerSecond, CurrentConfig.UdpRateLimitWindowSeconds),
                Logger = _logger
            };
            tasks.Add(_udpServer.Start(stoppingToken));
            _logger.LogInformation("Discard UDP server starting on {Address}:{Port}", config.ListenAddress, config.UdpPort);
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
            _logger.LogInformation("Discard service stopped");
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
