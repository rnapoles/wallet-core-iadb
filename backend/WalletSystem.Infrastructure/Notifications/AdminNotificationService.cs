using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Contracts.Services.Notification;

namespace WalletSystem.Infrastructure.Notifications;

public class AdminNotificationService : IAdminNotificationService
{
    private readonly IMessenger _messenger;
    private readonly string _adminEmail;
    private readonly bool _enabled;
    private readonly ILogger<AdminNotificationService> _logger;

    public AdminNotificationService(IMessenger messenger, IConfiguration configuration, ILogger<AdminNotificationService> logger)
    {
        _messenger = messenger;
        _enabled = configuration.GetValue<bool>("AdminNotification:Enabled", true);
        _adminEmail = configuration["AdminNotification:EmailAddress"] ?? "admin@wallet.loc";
        _logger = logger;
    }

    public async Task SendTransactionAlertAsync(string eventType, string details)
    {
        if (!_enabled)
        {
            _logger.LogDebug("Admin notifications are disabled. Skipping alert for {EventType}", eventType);
            return;
        }

        var subject = $"Wallet Alert: {eventType}";
        var body = $@"
<html>
<body>
    <h2>Transaction Alert</h2>
    <p><strong>Event:</strong> {eventType}</p>
    <p><strong>Details:</strong> {details}</p>
    <p><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>
</body>
</html>";

        _logger.LogInformation("Sending admin alert email to {Email} for event {EventType}", _adminEmail, eventType);
        await _messenger.SendAsync(_adminEmail, subject, body);
    }
}

