using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SuperServer.Services;

/// <summary>
/// Handles SIGHUP signals to reload configuration without restarting the service.
/// </summary>
public class ConfigurationReloadService : BackgroundService
{
    private readonly ILogger<ConfigurationReloadService> _logger;
    private readonly IConfiguration _configuration;

    public ConfigurationReloadService(
        ILogger<ConfigurationReloadService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // SIGHUP handling is only available on Unix-like systems
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _logger.LogDebug("SIGHUP configuration reload not available on this platform");
            return;
        }

        _logger.LogInformation("Configuration reload service started. Send SIGHUP to reload configuration");

        try
        {
            using var sigHup = PosixSignalRegistration.Create(PosixSignal.SIGHUP, OnSigHup);

            // Keep the service running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private void OnSigHup(PosixSignalContext context)
    {
        _logger.LogInformation("Received SIGHUP signal, reloading configuration...");

        try
        {
            if (_configuration is IConfigurationRoot configRoot)
            {
                configRoot.Reload();
                _logger.LogInformation("Configuration reloaded successfully");
            }
            else
            {
                _logger.LogWarning("Configuration root not available, cannot reload");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration: {Message}", ex.Message);
        }

        // Prevent the default signal handling (process termination)
        context.Cancel = true;
    }
}
