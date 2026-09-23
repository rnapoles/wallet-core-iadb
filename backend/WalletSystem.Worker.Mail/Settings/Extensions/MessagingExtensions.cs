using MassTransit;
using Microsoft.Extensions.Options;
using WalletSystem.Infrastructure.EventBus;
using WalletSystem.Infrastructure.Messaging.Smtp;
using WalletSystem.Worker.Mail.Consumers;

namespace WalletSystem.Worker.Mail.Settings.Extensions;

/// <summary>
/// Extension methods for configuring messaging services with MassTransit.
/// Supports switching between RabbitMQ and In-Memory transport based on configuration.
/// </summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Configures MassTransit messaging with support for RabbitMQ or In-Memory transport.
    /// Includes DLQ handling, exponential backoff retries, and scheduled redelivery.
    /// </summary>
    public static IServiceCollection AddMessagingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind settings
        // services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQSettings"));

        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        
        // Bind settings using Options pattern
        services.AddOptions<RabbitMqSettings>()
            .Bind(configuration.GetSection("RabbitMQSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register a direct singleton instance for MassTransit to resolve
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value);
        
        // Get messaging mode (RabbitMQ or InMemory)
        var messagingMode = configuration.GetValue<string>("Messaging:Mode") ?? "InMemory";

        services.AddMassTransit(x =>
        {
            x.AddConsumersFromNamespaceContaining<DepositCompletedMailConsumer>();
            x.AddConsumersFromNamespaceContaining<WithdrawalCompletedMailConsumer>();

            // x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("mail-worker", false));

            if (messagingMode.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
            {
                // Turn off anonymous data collection
                x.DisableUsageTelemetry();
                
                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitMqSettings = context.GetRequiredService<RabbitMqSettings>();

                    cfg.Host(rabbitMqSettings.Host, "/", h =>
                    {
                        h.Username(rabbitMqSettings.Username);
                        h.Password(rabbitMqSettings.Password);
                    });

                    // Configure Dead Letter Queue (DLQ)
                    cfg.UseDelayedRedelivery(r =>
                    {
                        r.Intervals(1000, 5000, 15000, 60000, 300000); // 1s, 5s, 15s, 1m, 5m
                        r.Handle<Exception>();
                    });

                    // Exponential backoff retry policy
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

                    // DLQ endpoint - messages go here after all retries exhausted
                    cfg.ReceiveEndpoint("mail-worker-dlq", e =>
                    {
                        e.ConfigureConsumeTopology = false;
                        e.Bind("mail-worker-deposit-completed-dlq");
                        e.Bind("mail-worker-withdrawal-completed-dlq");
});

                    // Deposit completed consumer endpoint
                    cfg.ReceiveEndpoint("mail-worker-deposit-completed", e =>
                    {
                        e.UseScheduledRedelivery(r => r.Intervals(1000, 5000, 15000, 60000, 300000));
                        e.ConfigureConsumer<DepositCompletedMailConsumer>(context);
                    });

                    // Withdrawal completed consumer endpoint
                    cfg.ReceiveEndpoint("mail-worker-withdrawal-completed", e =>
                    {
                        e.UseScheduledRedelivery(r => r.Intervals(1000, 5000, 15000, 60000, 300000));
                        e.ConfigureConsumer<WithdrawalCompletedMailConsumer>(context);
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
        });

        return services;
    }
}
