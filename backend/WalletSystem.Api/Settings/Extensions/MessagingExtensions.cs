using MassTransit;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Api.Settings.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        
        // Get (MySQL or SQLite)
        var databaseEngine = configuration.GetValue<string>("Database:Engine") ?? "SQLite";
        var useSqlite = "SQLite".Equals(databaseEngine, StringComparison.OrdinalIgnoreCase);
        
        // Get messaging mode (RabbitMQ or InMemory)
        var messagingMode = configuration.GetValue<string>("Messaging:Mode") ?? "InMemory";

        // Configure MassTransit with Outbox Pattern
        services.AddMassTransit(x =>
        {

            // Turn off anonymous data collection
            x.DisableUsageTelemetry();

            if (messagingMode.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitMqSettings = configuration.GetSection("RabbitMQSettings");
                    cfg.Host(rabbitMqSettings["Host"] ?? "localhost", h =>
                    {
                        h.Username(rabbitMqSettings["Username"] ?? "guest");
                        h.Password(rabbitMqSettings["Password"] ?? "guest");
                    });

                    // Configure Dead Letter Queue (DLQ) for failed messages
                    cfg.UseDelayedRedelivery(r =>
                    {
                        r.Intervals(1000, 5000, 15000, 60000, 300000); // 1s, 5s, 15s, 1m, 5m
                        r.Handle<Exception>();
                    });

                    cfg.UseMessageRetry(r =>
                    {
                        r.Exponential(
                            retryLimit: 5,
                            TimeSpan.FromSeconds(2),
                            TimeSpan.FromMinutes(5),
                            TimeSpan.FromSeconds(2)
                        );
                        r.Handle<Exception>();
                    });

                });
            }
            else
            {
                // In-Memory Transport for local development/testing
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }

            // Configure Outbox Pattern with Entity Framework Core
            // Configure Entity Framework Integration for the Outbox
            x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
            {

                if (useSqlite)
                {
                    // Use Sqlite syntax for outbox tables
                    o.UseSqlite();                    
                }
                else
                {
                    o.UseMySql();
                }
        
                // Automatically scan and delete successfully delivered messages
                o.QueryDelay = TimeSpan.FromSeconds(10);
        
                // Enable the outbox delivery service (the background worker)
                o.UseBusOutbox();
            });
        });

        return services;
    }
}

