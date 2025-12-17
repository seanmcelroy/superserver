using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SuperServer.HealthChecks;

/// <summary>
/// Health check that verifies a TCP port is accepting connections.
/// </summary>
public class TcpHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _serviceName;

    public TcpHealthCheck(string serviceName, string host, int port)
    {
        _serviceName = serviceName;
        _host = host;
        _port = port;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            await client.ConnectAsync(_host, _port, cts.Token);

            return HealthCheckResult.Healthy($"{_serviceName} TCP is accepting connections on port {_port}");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy($"{_serviceName} TCP connection timed out on port {_port}");
        }
        catch (SocketException ex)
        {
            return HealthCheckResult.Unhealthy($"{_serviceName} TCP is not accepting connections on port {_port}: {ex.Message}");
        }
    }
}
