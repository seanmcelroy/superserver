using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperServer.Configuration;
using SuperServer.Protocols.CharacterGenerator;

namespace SuperServer.Services;

public class CharGenService : BackgroundService
{
    private readonly ILogger<CharGenService> _logger;
    private readonly IOptionsMonitor<ServerConfiguration> _optionsMonitor;
    private readonly IDisposable? _optionsChangeToken;
    private CharGenTcpServer? _tcpServer;
    private CharGenUdpServer? _udpServer;

    // Store initial config for detecting restart-required changes
    private readonly string _initialListenAddress;
    private readonly ushort _initialTcpPort;
    private readonly ushort _initialUdpPort;

    public CharGenService(ILogger<CharGenService> logger, IOptionsMonitor<ServerConfiguration> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;

        var config = _optionsMonitor.CurrentValue.CharGen;
        _initialListenAddress = config.ListenAddress;
        _initialTcpPort = config.TcpPort;
        _initialUdpPort = config.UdpPort;

        _optionsChangeToken = _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(ServerConfiguration config, string? name)
    {
        var charGenConfig = config.CharGen;

        // Check for changes that require restart
        if (charGenConfig.ListenAddress != _initialListenAddress ||
            charGenConfig.TcpPort != _initialTcpPort ||
            charGenConfig.UdpPort != _initialUdpPort)
        {
            _logger.LogWarning(
                "CharGen service configuration changed (address/port). Restart required to apply: " +
                "ListenAddress={NewAddress} (was {OldAddress}), TcpPort={NewTcpPort} (was {OldTcpPort}), " +
                "UdpPort={NewUdpPort} (was {OldUdpPort})",
                charGenConfig.ListenAddress, _initialListenAddress,
                charGenConfig.TcpPort, _initialTcpPort,
                charGenConfig.UdpPort, _initialUdpPort);
        }

        // Log rate limit changes that are applied immediately
        _logger.LogInformation(
            "CharGen service configuration reloaded. Rate limits: {MaxRps} req/s, {Window}s window",
            charGenConfig.UdpMaxRequestsPerSecond?.ToString() ?? "unlimited",
            charGenConfig.UdpRateLimitWindowSeconds);
    }

    private CharGenConfiguration CurrentConfig => _optionsMonitor.CurrentValue.CharGen;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = CurrentConfig;

        if (!config.Enabled)
        {
            _logger.LogInformation("CharGen service is disabled");
            return;
        }

        if (!config.TcpEnabled && !config.UdpEnabled)
        {
            _logger.LogInformation("CharGen service has both TCP and UDP disabled");
            return;
        }

        if (!IPAddress.TryParse(config.ListenAddress, out var listenAddress))
        {
            _logger.LogError("Invalid listen address '{Address}' for CharGen service", config.ListenAddress);
            return;
        }

        var tasks = new List<Task>();

        if (config.TcpEnabled)
        {
            _tcpServer = new CharGenTcpServer
            {
                ListenAddress = listenAddress,
                ListenPort = config.TcpPort,
                TcpMaxConnections = config.TcpMaxConnections,
                IdleTimeoutSeconds = config.TcpIdleTimeoutSeconds,
                Logger = _logger
            };
            tasks.Add(_tcpServer.Start(stoppingToken));
            _logger.LogInformation("CharGen TCP server starting on {Address}:{Port}", config.ListenAddress, config.TcpPort);
        }

        if (config.UdpEnabled)
        {
            _logger.LogWarning(
                "CharGen UDP server is enabled. This protocol has severe amplification attack risk " +
                "(up to 512x amplification). Ensure this is intentional and rate limiting is configured.");
            _udpServer = new CharGenUdpServer
            {
                ListenAddress = listenAddress,
                ListenPort = config.UdpPort,
                ConfigurationProvider = () => (CurrentConfig.UdpMaxRequestsPerSecond, CurrentConfig.UdpRateLimitWindowSeconds),
                Logger = _logger
            };
            tasks.Add(_udpServer.Start(stoppingToken));
            _logger.LogInformation("CharGen UDP server starting on {Address}:{Port}", config.ListenAddress, config.UdpPort);
        }
        else
        {
            _logger.LogDebug("CharGen UDP server disabled (default for security - amplification attack mitigation)");
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
            _logger.LogInformation("CharGen service stopped");
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
