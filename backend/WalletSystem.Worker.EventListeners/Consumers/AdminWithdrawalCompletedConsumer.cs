using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Notification;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class AdminWithdrawalCompletedConsumer : IConsumer<WithdrawalCompleted>
{
    private readonly IAdminNotificationService _notificationService;
    private readonly ILogger<AdminWithdrawalCompletedConsumer> _logger;

    public AdminWithdrawalCompletedConsumer(IAdminNotificationService notificationService, ILogger<AdminWithdrawalCompletedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WithdrawalCompleted> context)
    {
        var message = context.Message;
        var details = $"Admin Event captured - Withdrawal completed: TransactionId={message.TransactionId}, WalletId={message.WalletId}, Amount={message.Amount}, NewBalance={message.NewBalance}";

        _logger.LogInformation(details);
        _logger.LogInformation("Sending admin notification for withdrawal: {TransactionId}", message.TransactionId);
        await _notificationService.SendTransactionAlertAsync("WithdrawalCompleted", details);
    }
}

