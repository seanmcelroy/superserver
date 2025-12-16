using System.Net;
using System.Net.Sockets;

namespace SuperServer;

public abstract class UdpServerBase : IDisposable
{
    public required IPAddress ListenAddress { get; init; }
    public required ushort ListenPort { get; init; }

    private UdpClient? _client;
    private bool disposedValue;

    public virtual async Task Start(CancellationToken cancellationToken)
    {
        _client = new UdpClient(new IPEndPoint(ListenAddress, ListenPort));
        await ProcessLoop(_client, cancellationToken);
    }

    protected abstract Task ProcessLoop(UdpClient client, CancellationToken cancellationToken);

    public void Stop()
    {
        _client?.Close();
        _client = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects)
                _client?.Dispose();
                _client = null;
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