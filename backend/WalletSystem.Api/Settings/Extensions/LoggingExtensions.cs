using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;

namespace WalletSystem.Api.Settings.Extensions;

public static class LoggingExtensions
{
    public static ConfigureHostBuilder ConfigureSerilog(this ConfigureHostBuilder host, IConfiguration configuration)
    {

        Serilog.Debugging.SelfLog.Enable(Console.Error);

        // Configure the global static logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            //.WriteTo.Console()
            .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentUserName()
            .WriteTo.Seq(configuration["Serilog:SeqServerUrl"] ?? "http://log-server:5341") // Write to Seq server
            .CreateBootstrapLogger();
        
        host.UseSerilog((context, services, config) => 
            config
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithClientIp()
                .Enrich.WithCorrelationId()
        );

        return host;
    }
}
