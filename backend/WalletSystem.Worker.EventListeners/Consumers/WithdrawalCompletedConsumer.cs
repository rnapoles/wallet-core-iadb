using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class WithdrawalCompletedConsumer : IConsumer<WithdrawalCompleted>
{
    private readonly ILogger<WithdrawalCompletedConsumer> _logger;

    public WithdrawalCompletedConsumer(ILogger<WithdrawalCompletedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WithdrawalCompleted> context)
    {
        var message = context.Message;
        _logger.LogInformation("Event captured - Withdrawal completed: {TransactionId}, WalletId: {WalletId}, Amount: {Amount}, NewBalance: {NewBalance}",
            message.TransactionId, message.WalletId, message.Amount, message.NewBalance);

        // Send withdrawal confirmation
        // Fraud detection for large withdrawals
        // Compliance reporting

        return Task.CompletedTask;
    }
}


