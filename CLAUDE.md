# superserver

Super Server (SS#) is an inetd-style internet daemon that implements several
early Internet RFCs as a monolithic .NET 9 application using C#. Rather than
launching external processes like inetd/xinetd, protocols are implemented
directly in code with async I/O.

## Protocols

### Echo (RFC 862)
Reads data from a client and echoes it back verbatim.
- **Ports**: TCP/2007, UDP/2007 (standard: 7)
- **UDP disabled by default** — reflection/amplification attack risk

### Discard (RFC 863)
Accepts data and silently discards it (network `/dev/null`).
- **Ports**: TCP/2009, UDP/2009 (standard: 9)
- UDP enabled by default (inherently safe, no response sent)

### Character Generator (RFC 864)
Continuously sends pseudo-random printable ASCII characters.
- **Ports**: TCP/2019, UDP/2019 (standard: 19)
- **UDP disabled by default** — severe amplification risk (up to 512x)

### Daytime (RFC 867)
Returns the current UTC time as a formatted string, then closes.
- **Ports**: TCP/2013, UDP/2013 (standard: 13)
- Supports configurable culture and format specifier

## Architecture

- `TcpServerBase` — connection semaphore, idle timeouts, metered streams
- `UdpServerBase` — per-IP sliding-window rate limiting
- Each protocol has a `BackgroundService` that manages TCP/UDP server lifecycle
- Configuration via `appsettings.json` with SIGHUP live reload
- Prometheus metrics at `/metrics`, health checks at `/health` on port 8080
- systemd integration with `install.sh`/`uninstall.sh`

## Building and Running

Requires .NET 9.0 SDK.

```bash
dotnet build
dotnet run
```

## Configuration

All protocol settings (ports, listen address, enable/disable, connection
limits, rate limits, idle timeouts) are in `appsettings.json`. SIGHUP reloads
rate limit changes live; port/address changes require a restart.
