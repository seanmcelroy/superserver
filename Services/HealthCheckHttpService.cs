using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperServer.Configuration;

namespace SuperServer.Services;

/// <summary>
/// Background service that exposes health check results via HTTP.
/// </summary>
public class HealthCheckHttpService : BackgroundService
{
    private readonly ILogger<HealthCheckHttpService> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly HealthCheckConfiguration _config;
    private HttpListener? _listener;

    public HealthCheckHttpService(
        ILogger<HealthCheckHttpService> logger,
        HealthCheckService healthCheckService,
        IOptions<HealthCheckConfiguration> options)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _config = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Health check HTTP endpoint is disabled");
            return;
        }

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://{_config.ListenAddress}:{_config.Port}/");

        try
        {
            _listener.Start();
            _logger.LogInformation("Health check endpoint listening on http://{Address}:{Port}/health, metrics on /metrics",
                _config.ListenAddress, _config.Port);
        }
        catch (HttpListenerException ex)
        {
            _logger.LogError(ex, "Failed to start health check HTTP listener: {Message}", ex.Message);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(stoppingToken);
                _ = HandleRequestAsync(context, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling health check request: {Message}", ex.Message);
            }
        }

        _listener.Stop();
        _logger.LogInformation("Health check endpoint stopped");
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url?.AbsolutePath == "/health" || request.Url?.AbsolutePath == "/")
            {
                var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
                await WriteHealthResponse(response, report);
            }
            else if (request.Url?.AbsolutePath == "/health/live")
            {
                // Liveness check - just returns 200 if the service is running
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                var buffer = Encoding.UTF8.GetBytes("OK");
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, cancellationToken);
            }
            else if (request.Url?.AbsolutePath == "/health/ready")
            {
                // Readiness check - same as full health check
                var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
                await WriteHealthResponse(response, report);
            }
            else if (request.Url?.AbsolutePath == "/metrics")
            {
                // Prometheus metrics endpoint
                await WriteMetricsResponse(response, cancellationToken);
            }
            else
            {
                response.StatusCode = 404;
                response.ContentType = "text/plain";
                var buffer = Encoding.UTF8.GetBytes("Not Found");
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing health check request");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }
    }

    private static async Task WriteHealthResponse(HttpListenerResponse response, HealthReport report)
    {
        response.StatusCode = report.Status == HealthStatus.Healthy ? 200 :
                              report.Status == HealthStatus.Degraded ? 200 : 503;
        response.ContentType = "application/json";

        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds
                })
        };

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
    }

    private static async Task WriteMetricsResponse(HttpListenerResponse response, CancellationToken cancellationToken)
    {
        response.StatusCode = 200;
        response.ContentType = "text/plain; version=0.0.4; charset=utf-8";

        await using var stream = response.OutputStream;
        await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream, cancellationToken);
    }

    public override void Dispose()
    {
        _listener?.Close();
        base.Dispose();
    }
}
