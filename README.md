# superserver

Because sometimes terrible ideas deserve to be reimplemented.

This is a simple inetd-style project that implements several RFCs from the early Internet, namely:

* RFC 862 - Echo Protocol                - tcp/2007 (instead of tcp/7) and udp/2007 (instead of udp/7)
* RFC 863 - Discard Protocol             - tcp/2009 (instead of tcp/9) and udp/2009 (instead of udp/9)
* RFC 864 - Character Generator Protocol - tcp/2019 (instead of tcp/19) and udp/2019 (instead of udp/19)
* RFC 867 - Daytime Protocol             - tcp/2013 (instead of tcp/13) and udp/2013 (instead of udp/13)


superserver is spiritually similar to inetd/xinetd but architecturally quite different. Rather than being a generic process launcher, it's a monolithic daemon with protocols implemented directly in code. This gives it better observability and efficiency (no fork overhead) but loses the flexibility of running arbitrary external programs. It's essentially what you'd get if you reimagined inetd for the modern cloud-native era with Prometheus, health checks, and async I/O - but scoped to a handful of toy protocols.       

It is gold-plated beyond belief, with systemd unit files, SIGHUP configuration reload support, health checks, and Prometheus metrics.

## Security notice

Many of these early protocols have little to no utility on the modern Internet.  Worse, some can reveal
information about a host's state which can be security sensitive, or they can allow for amplification attacks
when packets are spoofed.  This server should be considered a hobbyist/experimental project and not deployed,
especially at scale.

## Building

Requires .NET 9.0 SDK.

```bash
dotnet build
```

## Running (Development)

```bash
dotnet run
```

## Deployment (Linux systemd)

### Installation

```bash
sudo ./install.sh
```

This will:
- Create a `superserver` system user and group
- Build and copy binaries to `/opt/superserver`
- Copy configuration to `/etc/superserver/appsettings.json`
- Install and enable the systemd service

### Managing the Service

```bash
# Start the service
sudo systemctl start superserver

# Stop the service
sudo systemctl stop superserver

# Restart the service
sudo systemctl restart superserver

# Check status
sudo systemctl status superserver

# View logs
sudo journalctl -u superserver -f

# Enable on boot (done by install.sh)
sudo systemctl enable superserver

# Disable on boot
sudo systemctl disable superserver
```

### Configuration

Edit `/etc/superserver/appsettings.json` to configure:
- Enable/disable individual protocols
- Change TCP/UDP ports
- Change listen addresses

After editing, restart the service:
```bash
sudo systemctl restart superserver
```

### Using Privileged Ports

To use the standard RFC ports (7, 9, 13, 19) instead of the unprivileged defaults (2007, 2009, 2013, 2019), simply update the port numbers in `/etc/superserver/appsettings.json`:

```json
{
  "Servers": {
    "Echo": { "TcpPort": 7, "UdpPort": 7, ... },
    "Discard": { "TcpPort": 9, "UdpPort": 9, ... },
    "Daytime": { "TcpPort": 13, "UdpPort": 13, ... },
    "CharGen": { "TcpPort": 19, "UdpPort": 19, ... }
  }
}
```

The systemd service is configured with `CAP_NET_BIND_SERVICE` capability, allowing it to bind to privileged ports (below 1024) without running as root.

### Health Checks

The service exposes HTTP health check endpoints on port 8080 (configurable):

```bash
# Full health check (checks all enabled TCP listeners)
curl http://127.0.0.1:8080/health

# Liveness probe (returns 200 if service is running)
curl http://127.0.0.1:8080/health/live

# Readiness probe (same as full health check)
curl http://127.0.0.1:8080/health/ready
```

Example response:
```json
{
  "status": "Healthy",
  "totalDuration": 12.34,
  "entries": {
    "echo-tcp": { "status": "Healthy", "description": "...", "duration": 1.5 },
    "discard-tcp": { "status": "Healthy", "description": "...", "duration": 1.2 }
  }
}
```

Configure health checks in `appsettings.json`:
```json
{
  "HealthCheck": {
    "Enabled": true,
    "ListenAddress": "127.0.0.1",
    "Port": 8080
  }
}
```

### Uninstallation

```bash
sudo ./uninstall.sh
```

This will stop and remove the service, delete binaries, and optionally remove configuration and the service user.