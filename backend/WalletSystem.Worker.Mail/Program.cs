using Serilog;
using WalletSystem.Shared.Settings.Extensions;
using WalletSystem.Worker.Mail.Settings.Extensions;

namespace WalletSystem.Worker.Mail;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Configuration priority (highest to lowest):
                    // 1. Environment Variables
                    // 2. .env file
                    // 3. appsettings.json
                    config.AddEnvironmentFile();
                    config.AddEnvironmentVariables();
                })
                .UseSerilog((context, services, configuration) =>
                {
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services using extension methods
                    services.AddApplicationServices();
                    services.AddMessagingServices(hostContext.Configuration);
                    services.AddWorkerServices();
                })
                .Build();

            host.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlushAsync().GetAwaiter().GetResult();
        }
    }
}
