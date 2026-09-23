using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Notification;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class AdminTransactionCreatedConsumer : IConsumer<TransactionCreated>
{
    private readonly IAdminNotificationService _notificationService;
    private readonly ILogger<AdminTransactionCreatedConsumer> _logger;

    public AdminTransactionCreatedConsumer(IAdminNotificationService notificationService, ILogger<AdminTransactionCreatedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreated> context)
    {
        var message = context.Message;
        var details = $"Event captured - Transaction created: TransactionId={message.TransactionId}, WalletId={message.WalletId}, Type={message.Type}, Amount={message.Amount}";

        _logger.LogInformation("Sending admin notification for new transaction: {TransactionId}", message.TransactionId);
        await _notificationService.SendTransactionAlertAsync("TransactionCreated", details);
    }
}

