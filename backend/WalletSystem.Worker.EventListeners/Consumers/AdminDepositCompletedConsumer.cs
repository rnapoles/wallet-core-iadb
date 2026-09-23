using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class AdminDepositCompletedConsumer : IConsumer<DepositCompleted>
{
    private readonly ILogger<AdminDepositCompletedConsumer> _logger;

    public AdminDepositCompletedConsumer(ILogger<AdminDepositCompletedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<DepositCompleted> context)
    {
        var message = context.Message;
        _logger.LogInformation("Event captured - Deposit completed: {TransactionId}, WalletId: {WalletId}, Amount: {Amount}, NewBalance: {NewBalance}",
            message.TransactionId, message.WalletId, message.Amount, message.NewBalance);

        // Send deposit confirmation email/notification
        // Update analytics
        // Trigger loyalty points calculation

        return Task.CompletedTask;
    }
}


