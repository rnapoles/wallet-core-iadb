using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Contracts.Services.Notification;

namespace WalletSystem.Infrastructure.Messaging.Smtp;

public class SmtpMessenger : IMessenger
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpMessenger> _logger;

    public SmtpMessenger(SmtpSettings settings, ILogger<SmtpMessenger> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port);
        client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        client.EnableSsl = _settings.EnableSsl;

        var from = new MailAddress(_settings.From, _settings.FromName);
        var message = new MailMessage(from, new MailAddress(to))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        _logger.LogInformation("[SmtpMessenger] Sending email to {To} with subject: {Subject}", to, subject);

        await client.SendMailAsync(message, cancellationToken);

        _logger.LogInformation("[SmtpMessenger] Email sent successfully to {To}", to);
    }
}
