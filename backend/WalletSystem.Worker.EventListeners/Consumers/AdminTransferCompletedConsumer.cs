using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Notification;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class AdminTransferCompletedConsumer : IConsumer<TransferCompleted>
{
    private readonly IAdminNotificationService _notificationService;
    private readonly ILogger<AdminTransferCompletedConsumer> _logger;

    public AdminTransferCompletedConsumer(IAdminNotificationService notificationService, ILogger<AdminTransferCompletedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransferCompleted> context)
    {
        var message = context.Message;
        var details = $"Event captured - Transfer completed: TransactionId={message.TransactionId}, FromWalletId={message.FromWalletId}, ToWalletId={message.ToWalletId}, Amount={message.Amount}";

        _logger.LogInformation("Sending admin notification for transfer: {TransactionId}", message.TransactionId);
        await _notificationService.SendTransactionAlertAsync("TransferCompleted", details);
    }
}

