using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SuperServer.Metrics;

namespace SuperServer;

public abstract class UdpServerBase : IDisposable
{
    public required IPAddress ListenAddress { get; init; }
    public required ushort ListenPort { get; init; }
    public ILogger? Logger { get; init; }

    /// <summary>
    /// The protocol name used for metrics labels (e.g., "echo", "discard").
    /// </summary>
    public abstract string ProtocolName { get; }

    /// <summary>
    /// Maximum UDP requests per second per source IP. Set to null for unlimited (not recommended).
    /// When ConfigurationProvider is set, this value is ignored and the provider is used instead.
    /// </summary>
    public int? MaxRequestsPerSecond { get; init; } = 100;

    /// <summary>
    /// Time window in seconds for rate limiting. Requests are counted within this sliding window.
    /// When ConfigurationProvider is set, this value is ignored and the provider is used instead.
    /// </summary>
    public int RateLimitWindowSeconds { get; init; } = 1;

    /// <summary>
    /// Optional delegate to provide dynamic configuration. When set, rate limit values are
    /// read from this provider on each request, enabling live configuration updates.
    /// </summary>
    public Func<(int? MaxRequestsPerSecond, int RateLimitWindowSeconds)>? ConfigurationProvider { get; init; }

    private UdpClient? _client;
    private bool _disposedValue;

    // Rate limiting state
    private readonly ConcurrentDictionary<IPAddress, RateLimitEntry> _rateLimitEntries = new();
    private DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

    private class RateLimitEntry
    {
        public int RequestCount;
        public DateTime WindowStart;
        public readonly object Lock = new();
    }

    public virtual async Task Start(CancellationToken cancellationToken)
    {
        try
        {
            _client = new UdpClient(new IPEndPoint(ListenAddress, ListenPort));
            Logger?.LogDebug("UDP listener started on {Address}:{Port}", ListenAddress, ListenPort);
        }
        catch (SocketException ex)
        {
            Logger?.LogError(ex, "Failed to start UDP listener on {Address}:{Port}: {Message}",
                ListenAddress, ListenPort, ex.Message);
            throw;
        }

        await ProcessLoop(_client, cancellationToken);
    }

    protected abstract Task ProcessLoop(UdpClient client, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current rate limit configuration, either from the dynamic provider or static properties.
    /// </summary>
    private (int? MaxRequestsPerSecond, int RateLimitWindowSeconds) GetRateLimitConfig()
    {
        if (ConfigurationProvider != null)
            return ConfigurationProvider();
        return (MaxRequestsPerSecond, RateLimitWindowSeconds);
    }

    /// <summary>
    /// Checks if the given IP address is rate limited. Returns true if the request should be dropped.
    /// </summary>
    protected bool IsRateLimited(IPAddress remoteAddress)
    {
        var (maxRequestsPerSecond, rateLimitWindowSeconds) = GetRateLimitConfig();

        if (!maxRequestsPerSecond.HasValue)
            return false;

        var now = DateTime.UtcNow;
        var windowDuration = TimeSpan.FromSeconds(rateLimitWindowSeconds);

        // Periodic cleanup of stale entries
        if (now - _lastCleanup > CleanupInterval)
        {
            CleanupStaleEntries(now, windowDuration);
            _lastCleanup = now;
        }

        var entry = _rateLimitEntries.GetOrAdd(remoteAddress, _ => new RateLimitEntry
        {
            RequestCount = 0,
            WindowStart = now
        });

        lock (entry.Lock)
        {
            // Check if we're still in the same window
            if (now - entry.WindowStart > windowDuration)
            {
                // Start a new window
                entry.WindowStart = now;
                entry.RequestCount = 1;
                return false;
            }

            // Increment and check
            entry.RequestCount++;
            var maxRequests = maxRequestsPerSecond.Value * rateLimitWindowSeconds;

            if (entry.RequestCount > maxRequests)
            {
                ServerMetrics.RateLimitedTotal.WithLabels(ProtocolName, "udp").Inc();
                Logger?.LogWarning("Rate limit exceeded for {RemoteAddress}: {Count} requests in {Window}s (max: {Max})",
                    remoteAddress, entry.RequestCount, rateLimitWindowSeconds, maxRequests);
                return true;
            }

            return false;
        }
    }

    private void CleanupStaleEntries(DateTime now, TimeSpan windowDuration)
    {
        var staleThreshold = now - windowDuration - TimeSpan.FromSeconds(10);
        var staleKeys = _rateLimitEntries
            .Where(kvp => kvp.Value.WindowStart < staleThreshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            _rateLimitEntries.TryRemove(key, out _);
        }

        if (staleKeys.Count > 0)
        {
            Logger?.LogDebug("Cleaned up {Count} stale rate limit entries", staleKeys.Count);
        }
    }

    /// <summary>
    /// Tracks that a UDP request was received. Call this for each datagram processed.
    /// </summary>
    protected void TrackRequest()
    {
        ServerMetrics.RequestsTotal.WithLabels(ProtocolName, "udp").Inc();
    }

    /// <summary>
    /// Tracks bytes received from a UDP datagram.
    /// </summary>
    protected void TrackBytesReceived(int bytes)
    {
        if (bytes > 0)
        {
            ServerMetrics.BytesReceivedTotal.WithLabels(ProtocolName, "udp").Inc(bytes);
        }
    }

    /// <summary>
    /// Tracks bytes sent in a UDP response.
    /// </summary>
    protected void TrackBytesSent(int bytes)
    {
        if (bytes > 0)
        {
            ServerMetrics.BytesSentTotal.WithLabels(ProtocolName, "udp").Inc(bytes);
        }
    }

    /// <summary>
    /// Tracks an error that occurred while processing a UDP request.
    /// </summary>
    protected void TrackError()
    {
        ServerMetrics.ErrorsTotal.WithLabels(ProtocolName, "udp").Inc();
    }

    public void Stop()
    {
        _client?.Close();
        _client = null;
        _rateLimitEntries.Clear();
        Logger?.LogDebug("UDP listener stopped");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _client?.Dispose();
                _client = null;
                _rateLimitEntries.Clear();
            }
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
