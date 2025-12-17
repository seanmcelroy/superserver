using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperServer.Configuration;
using SuperServer.Protocols.Daytime;

namespace SuperServer.Services;

public class DaytimeService : BackgroundService
{
    private readonly ILogger<DaytimeService> _logger;
    private readonly IOptionsMonitor<ServerConfiguration> _optionsMonitor;
    private readonly IDisposable? _optionsChangeToken;
    private DaytimeTcpServer? _tcpServer;
    private DaytimeUdpServer? _udpServer;

    // Store initial config for detecting restart-required changes
    private readonly string _initialListenAddress;
    private readonly ushort _initialTcpPort;
    private readonly ushort _initialUdpPort;

    public DaytimeService(ILogger<DaytimeService> logger, IOptionsMonitor<ServerConfiguration> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;

        var config = _optionsMonitor.CurrentValue.Daytime;
        _initialListenAddress = config.ListenAddress;
        _initialTcpPort = config.TcpPort;
        _initialUdpPort = config.UdpPort;

        _optionsChangeToken = _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(ServerConfiguration config, string? name)
    {
        var daytimeConfig = config.Daytime;

        // Check for changes that require restart
        if (daytimeConfig.ListenAddress != _initialListenAddress ||
            daytimeConfig.TcpPort != _initialTcpPort ||
            daytimeConfig.UdpPort != _initialUdpPort)
        {
            _logger.LogWarning(
                "Daytime service configuration changed (address/port). Restart required to apply: " +
                "ListenAddress={NewAddress} (was {OldAddress}), TcpPort={NewTcpPort} (was {OldTcpPort}), " +
                "UdpPort={NewUdpPort} (was {OldUdpPort})",
                daytimeConfig.ListenAddress, _initialListenAddress,
                daytimeConfig.TcpPort, _initialTcpPort,
                daytimeConfig.UdpPort, _initialUdpPort);
        }

        // Log rate limit changes that are applied immediately
        _logger.LogInformation(
            "Daytime service configuration reloaded. Rate limits: {MaxRps} req/s, {Window}s window",
            daytimeConfig.UdpMaxRequestsPerSecond?.ToString() ?? "unlimited",
            daytimeConfig.UdpRateLimitWindowSeconds);
    }

    private DaytimeConfiguration CurrentConfig => _optionsMonitor.CurrentValue.Daytime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = CurrentConfig;

        if (!config.Enabled)
        {
            _logger.LogInformation("Daytime service is disabled");
            return;
        }

        if (!config.TcpEnabled && !config.UdpEnabled)
        {
            _logger.LogInformation("Daytime service has both TCP and UDP disabled");
            return;
        }

        if (!IPAddress.TryParse(config.ListenAddress, out var listenAddress))
        {
            _logger.LogError("Invalid listen address '{Address}' for Daytime service", config.ListenAddress);
            return;
        }

        var tasks = new List<Task>();

        if (config.TcpEnabled)
        {
            _tcpServer = new DaytimeTcpServer
            {
                ListenAddress = listenAddress,
                ListenPort = config.TcpPort,
                TcpMaxConnections = config.TcpMaxConnections,
                IdleTimeoutSeconds = config.TcpIdleTimeoutSeconds,
                Logger = _logger
            };
            tasks.Add(_tcpServer.Start(stoppingToken));
            _logger.LogInformation("Daytime TCP server starting on {Address}:{Port}", config.ListenAddress, config.TcpPort);
        }

        if (config.UdpEnabled)
        {
            _udpServer = new DaytimeUdpServer
            {
                ListenAddress = listenAddress,
                ListenPort = config.UdpPort,
                ConfigurationProvider = () => (CurrentConfig.UdpMaxRequestsPerSecond, CurrentConfig.UdpRateLimitWindowSeconds),
                Logger = _logger
            };
            tasks.Add(_udpServer.Start(stoppingToken));
            _logger.LogInformation("Daytime UDP server starting on {Address}:{Port}", config.ListenAddress, config.UdpPort);
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
            _logger.LogInformation("Daytime service stopped");
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
