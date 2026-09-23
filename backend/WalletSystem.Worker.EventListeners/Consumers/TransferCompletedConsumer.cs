using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class TransferCompletedConsumer : IConsumer<TransferCompleted>
{
    private readonly ILogger<TransferCompletedConsumer> _logger;

    public TransferCompletedConsumer(ILogger<TransferCompletedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TransferCompleted> context)
    {
        var message = context.Message;
        _logger.LogInformation("Event captured - Transfer completed: {TransactionId}, From: {FromWalletId}, To: {ToWalletId}, Amount: {Amount}",
            message.TransactionId, message.FromWalletId, message.ToWalletId, message.Amount);

        // Send transfer notifications to both parties
        // Update relationship graphs
        // Anti-money laundering checks

        return Task.CompletedTask;
    }
}


