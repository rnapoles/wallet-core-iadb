using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Notification;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class DepositCompletedConsumer : IConsumer<DepositCompleted>
{
    private readonly IAdminNotificationService _notificationService;
    private readonly ILogger<DepositCompletedConsumer> _logger;

    public DepositCompletedConsumer(IAdminNotificationService notificationService, ILogger<DepositCompletedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DepositCompleted> context)
    {
        var message = context.Message;
        var details = $"Event captured - Deposit completed: TransactionId={message.TransactionId}, WalletId={message.WalletId}, Amount={message.Amount}, NewBalance={message.NewBalance}";

        _logger.LogInformation("Sending admin notification for deposit: {TransactionId}", message.TransactionId);
        await _notificationService.SendTransactionAlertAsync("DepositCompleted", details);
    }
}

