using System.Net;
using System.Net.Sockets;

namespace SuperServer;

/// <summary>
/// This is a base class for all TCP servers that implement protocols in the Protocols/ directory.
/// </summary>
public abstract class TcpServerBase : IDisposable
{
    public required IPAddress ListenAddress { get; init; }
    public required ushort ListenPort { get; init; }

    private TcpListener? _listener { get; set;}
    private bool disposedValue;

    public virtual async Task Start(CancellationToken cancellationToken)
    {
        _listener = new TcpListener(ListenAddress, ListenPort);
        _listener.Start();

        await ListenLoop(cancellationToken);
    }

    private async Task ListenLoop(CancellationToken cancellationToken)
    {
        while (_listener != null && !cancellationToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            _ = HandleClientAsync(client, cancellationToken); // Fire and forget
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            var stream = client.GetStream();
            await HandleClientAsync(stream, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { } // Client disconnected unexpectedly
        finally
        {
            client.Dispose();
        }
    }

    protected abstract Task HandleClientAsync(NetworkStream stream, CancellationToken cancellationToken);

    public void Stop()
    {
        _listener?.Stop();
        _listener = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects)
                _listener?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}