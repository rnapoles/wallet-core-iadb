namespace WalletSystem.Application.Contracts.Services.Notification;

public interface IAdminNotificationService
{
    Task SendTransactionAlertAsync(string eventType, string details);
}
