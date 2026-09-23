using Microsoft.Extensions.Logging;
using WalletSystem.Application.Contracts.Services.Notification;

namespace WalletSystem.Infrastructure.Messaging.Smtp;

public class FakeMessenger : IMessenger
{
    private readonly ILogger<FakeMessenger> _logger;

    public FakeMessenger(ILogger<FakeMessenger> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[FakeMessenger] Sending email to {To} with subject: {Subject}", to, subject);
        _logger.LogDebug("[FakeMessenger] Body: {Body}", body);
        return Task.CompletedTask;
    }
}

