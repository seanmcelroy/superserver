using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuperServer.Configuration;
using SuperServer.HealthChecks;
using SuperServer.Services;

Host.CreateDefaultBuilder(args)
    .UseSystemd()
    .ConfigureServices((context, services) =>
    {
        // Bind configuration
        services.Configure<ServerConfiguration>(
            context.Configuration.GetSection("Servers"));
        services.Configure<HealthCheckConfiguration>(
            context.Configuration.GetSection("HealthCheck"));

        var serverConfig = context.Configuration.GetSection("Servers").Get<ServerConfiguration>()
            ?? new ServerConfiguration();

        // Register health checks for enabled services
        var healthChecks = services.AddHealthChecks();

        if (serverConfig.Echo.Enabled)
        {
            healthChecks.AddCheck("echo-tcp",
                new TcpHealthCheck("Echo", serverConfig.Echo.ListenAddress, serverConfig.Echo.TcpPort));
        }
        if (serverConfig.Discard.Enabled)
        {
            healthChecks.AddCheck("discard-tcp",
                new TcpHealthCheck("Discard", serverConfig.Discard.ListenAddress, serverConfig.Discard.TcpPort));
        }
        if (serverConfig.Daytime.Enabled)
        {
            healthChecks.AddCheck("daytime-tcp",
                new TcpHealthCheck("Daytime", serverConfig.Daytime.ListenAddress, serverConfig.Daytime.TcpPort));
        }
        if (serverConfig.CharGen.Enabled)
        {
            healthChecks.AddCheck("chargen-tcp",
                new TcpHealthCheck("CharGen", serverConfig.CharGen.ListenAddress, serverConfig.CharGen.TcpPort));
        }

        // Register protocol services
        services.AddHostedService<EchoService>();
        services.AddHostedService<DiscardService>();
        services.AddHostedService<DaytimeService>();
        services.AddHostedService<CharGenService>();

        // Register health check HTTP endpoint
        services.AddHostedService<HealthCheckHttpService>();

        // Register configuration reload service (handles SIGHUP)
        services.AddHostedService<ConfigurationReloadService>();
    })
    .Build()
    .Run();
