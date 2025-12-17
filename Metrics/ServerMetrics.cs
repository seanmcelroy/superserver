using Prometheus;

namespace SuperServer.Metrics;

/// <summary>
/// Prometheus metrics for the superserver.
/// </summary>
public static class ServerMetrics
{
    private static readonly string[] ProtocolLabels = ["protocol", "transport"];

    /// <summary>
    /// Total number of connections accepted (TCP only).
    /// </summary>
    public static readonly Counter ConnectionsTotal = Prometheus.Metrics.CreateCounter(
        "superserver_connections_total",
        "Total number of TCP connections accepted",
        new CounterConfiguration
        {
            LabelNames = ["protocol"]
        });

    /// <summary>
    /// Number of currently active connections (TCP only).
    /// </summary>
    public static readonly Gauge ConnectionsActive = Prometheus.Metrics.CreateGauge(
        "superserver_connections_active",
        "Number of currently active TCP connections",
        new GaugeConfiguration
        {
            LabelNames = ["protocol"]
        });

    /// <summary>
    /// Total number of requests processed.
    /// </summary>
    public static readonly Counter RequestsTotal = Prometheus.Metrics.CreateCounter(
        "superserver_requests_total",
        "Total number of requests processed",
        new CounterConfiguration
        {
            LabelNames = ProtocolLabels
        });

    /// <summary>
    /// Total bytes received.
    /// </summary>
    public static readonly Counter BytesReceivedTotal = Prometheus.Metrics.CreateCounter(
        "superserver_bytes_received_total",
        "Total bytes received",
        new CounterConfiguration
        {
            LabelNames = ProtocolLabels
        });

    /// <summary>
    /// Total bytes sent.
    /// </summary>
    public static readonly Counter BytesSentTotal = Prometheus.Metrics.CreateCounter(
        "superserver_bytes_sent_total",
        "Total bytes sent",
        new CounterConfiguration
        {
            LabelNames = ProtocolLabels
        });

    /// <summary>
    /// Total number of connection/request errors.
    /// </summary>
    public static readonly Counter ErrorsTotal = Prometheus.Metrics.CreateCounter(
        "superserver_errors_total",
        "Total number of errors",
        new CounterConfiguration
        {
            LabelNames = ProtocolLabels
        });

    /// <summary>
    /// Total number of connections/requests rejected due to rate limiting.
    /// </summary>
    public static readonly Counter RateLimitedTotal = Prometheus.Metrics.CreateCounter(
        "superserver_rate_limited_total",
        "Total number of requests rejected due to rate limiting",
        new CounterConfiguration
        {
            LabelNames = ProtocolLabels
        });

    /// <summary>
    /// Total number of connections closed due to idle timeout.
    /// </summary>
    public static readonly Counter IdleTimeoutsTotal = Prometheus.Metrics.CreateCounter(
        "superserver_idle_timeouts_total",
        "Total number of connections closed due to idle timeout",
        new CounterConfiguration
        {
            LabelNames = ["protocol"]
        });
}
