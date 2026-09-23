using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class TransactionCreatedConsumer : IConsumer<TransactionCreated>
{
    private readonly ILogger<TransactionCreatedConsumer> _logger;

    public TransactionCreatedConsumer(ILogger<TransactionCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TransactionCreated> context)
    {
        var message = context.Message;
        _logger.LogInformation("Event captured - Transaction created: {TransactionId}, Type: {Type}, Amount: {Amount}, WalletId: {WalletId}",
            message.TransactionId, message.Type, message.Amount, message.WalletId);

        // Here you can add additional processing logic like:
        // - Sending notifications
        // - Updating external systems
        // - Fraud detection checks
        // - Audit logging to external systems

        return Task.CompletedTask;
    }
}


