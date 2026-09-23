using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WalletSystem.Application.Contracts.Services.Notification;
using WalletSystem.Infrastructure.EventBus;
using WalletSystem.Infrastructure.Messaging.Smtp;
using WalletSystem.Infrastructure.Notifications;
using WalletSystem.Worker.EventListeners.Consumers;
using WalletSystem.Worker.EventListeners.Consumers.Dlq;

namespace WalletSystem.Worker.EventListeners.Settings.Extensions;

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
        
        // Register Messenger services
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.AddSingleton<IMessenger>(sp =>
        {
            //var settings = sp.GetRequiredService<IOptions<SmtpSettings>>().Value;
            var logger = sp.GetRequiredService<ILogger<FakeMessenger>>();
            return new FakeMessenger(logger);
        });

        // Register Admin Notification Service
        services.AddScoped<IAdminNotificationService, AdminNotificationService>();
        
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

            // Register all consumers from WalletSystem.Messaging and WalletSystem.Application assemblies
            x.AddConsumers(typeof(AdminDepositCompletedConsumer).Assembly);

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

                    // DLQ endpoint - receives messages after all retries are exhausted
                    cfg.ReceiveEndpoint("api-dlq", e =>
                    {
                        e.ConfigureConsumeTopology = false;
                        e.ConfigureConsumer<DlqConsumer>(context);
                        e.UseMessageRetry(r => r.None()); // Disable retries on DLQ
                    });

                    // Configure endpoints for deposit/withdrawal events with scheduled redelivery
                    cfg.ReceiveEndpoint("deposit-completed", e =>
                    {
                        e.ConfigureConsumeTopology = true;
                        e.UseScheduledRedelivery(r => r.Intervals(1000, 5000, 15000, 60000, 300000));
                        e.ConfigureConsumer<AdminDepositCompletedConsumer>(context);
                        e.ConfigureConsumer<DepositCompletedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("withdrawal-completed", e =>
                    {
                        e.ConfigureConsumeTopology = true;
                        e.UseScheduledRedelivery(r => r.Intervals(1000, 5000, 15000, 60000, 300000));
                        e.ConfigureConsumer<WithdrawalCompletedConsumer>(context);
                        e.ConfigureConsumer<AdminWithdrawalCompletedConsumer>(context);
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
