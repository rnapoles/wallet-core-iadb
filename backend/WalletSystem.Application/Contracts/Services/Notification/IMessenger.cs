namespace WalletSystem.Application.Contracts.Services.Notification;

/// <summary>
/// Abstraction for sending messages (email, SMS, etc.).
/// Defined in Application so use cases can trigger notifications
/// without coupling to SMTP or any other transport.
/// </summary>
public interface IMessenger
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
