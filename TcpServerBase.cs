using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SuperServer.Metrics;

namespace SuperServer;

/// <summary>
/// This is a base class for all TCP servers that implement protocols in the Protocols/ directory.
/// </summary>
public abstract class TcpServerBase : IDisposable
{
    public required IPAddress ListenAddress { get; init; }
    public required ushort ListenPort { get; init; }
    public ILogger? Logger { get; init; }

    /// <summary>
    /// The protocol name used for metrics labels (e.g., "echo", "discard").
    /// </summary>
    public abstract string ProtocolName { get; }

    /// <summary>
    /// Maximum number of concurrent connections. Set to null for unlimited (not recommended).
    /// </summary>
    public int? TcpMaxConnections { get; init; } = 100;

    /// <summary>
    /// Idle timeout in seconds for connections. Connections that don't receive data within
    /// this period will be closed. Set to null for no timeout (not recommended).
    /// </summary>
    public int? IdleTimeoutSeconds { get; init; } = 60;

    private TcpListener? _listener;
    private SemaphoreSlim? _connectionSemaphore;
    private bool _disposedValue;

    public virtual async Task Start(CancellationToken cancellationToken)
    {
        try
        {
            _listener = new TcpListener(ListenAddress, ListenPort);
            _listener.Start();

            if (TcpMaxConnections.HasValue)
            {
                _connectionSemaphore = new SemaphoreSlim(TcpMaxConnections.Value, TcpMaxConnections.Value);
                Logger?.LogDebug("TCP listener started on {Address}:{Port} (max {MaxConnections} connections)",
                    ListenAddress, ListenPort, TcpMaxConnections.Value);
            }
            else
            {
                Logger?.LogDebug("TCP listener started on {Address}:{Port} (unlimited connections)",
                    ListenAddress, ListenPort);
            }
        }
        catch (SocketException ex)
        {
            Logger?.LogError(ex, "Failed to start TCP listener on {Address}:{Port}: {Message}",
                ListenAddress, ListenPort, ex.Message);
            throw;
        }

        await ListenLoop(cancellationToken);
    }

    private async Task ListenLoop(CancellationToken cancellationToken)
    {
        while (_listener != null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for a connection slot if limiting is enabled
                if (_connectionSemaphore != null)
                {
                    await _connectionSemaphore.WaitAsync(cancellationToken);
                }

                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch
                {
                    // Release semaphore if we failed to accept
                    _connectionSemaphore?.Release();
                    throw;
                }

                _ = HandleClientWithLoggingAsync(client, cancellationToken);
            }
            catch (SocketException ex) when (!cancellationToken.IsCancellationRequested)
            {
                Logger?.LogWarning(ex, "Socket error accepting connection: {Message}", ex.Message);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected on shutdown, release semaphore if we were waiting
                break;
            }
        }
    }

    private async Task HandleClientWithLoggingAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var remoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Logger?.LogDebug("TCP connection accepted from {RemoteEndPoint}", remoteEndPoint);

        // Track metrics
        ServerMetrics.ConnectionsTotal.WithLabels(ProtocolName).Inc();
        ServerMetrics.ConnectionsActive.WithLabels(ProtocolName).Inc();

        var timedOut = false;
        try
        {
            var networkStream = client.GetStream();

            // Set read timeout if configured
            if (IdleTimeoutSeconds.HasValue)
            {
                networkStream.ReadTimeout = IdleTimeoutSeconds.Value * 1000;
            }

            // Wrap stream with MeteredStream for byte tracking
            var stream = new MeteredStream(networkStream, ProtocolName, "tcp");

            await HandleClientAsync(stream, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (IOException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut })
        {
            timedOut = true;
            ServerMetrics.IdleTimeoutsTotal.WithLabels(ProtocolName).Inc();
            Logger?.LogDebug("TCP connection from {RemoteEndPoint} closed due to idle timeout ({Timeout}s)",
                remoteEndPoint, IdleTimeoutSeconds);
        }
        catch (IOException) { }
        catch (Exception ex)
        {
            ServerMetrics.ErrorsTotal.WithLabels(ProtocolName, "tcp").Inc();
            Logger?.LogError(ex, "Unhandled exception handling client {RemoteEndPoint}: {Message}",
                remoteEndPoint, ex.Message);
        }
        finally
        {
            ServerMetrics.ConnectionsActive.WithLabels(ProtocolName).Dec();
            if (timedOut)
            {
                Logger?.LogDebug("TCP connection closed from {RemoteEndPoint} (idle timeout)", remoteEndPoint);
            }
            else
            {
                Logger?.LogDebug("TCP connection closed from {RemoteEndPoint}", remoteEndPoint);
            }
            client.Dispose();
            _connectionSemaphore?.Release();
        }
    }

    protected abstract Task HandleClientAsync(Stream stream, CancellationToken cancellationToken);

    public void Stop()
    {
        _listener?.Stop();
        _listener = null;
        Logger?.LogDebug("TCP listener stopped");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _listener?.Dispose();
                _connectionSemaphore?.Dispose();
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
