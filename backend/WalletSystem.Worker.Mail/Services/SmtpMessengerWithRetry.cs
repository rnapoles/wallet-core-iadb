using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using WalletSystem.Application.Contracts.Services.Notification;
using WalletSystem.Infrastructure.Messaging.Smtp;

namespace WalletSystem.Worker.Mail.Services;

/// <summary>
/// SMTP implementation of IMessenger with exponential backoff retry policy.
/// Uses Mailpit as the SMTP server in development environments.
/// </summary>
public class SmtpMessengerWithRetry : IMessenger
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpMessengerWithRetry> _logger;
    private readonly int _maxRetries;
    private readonly double _baseDelaySeconds;

    public SmtpMessengerWithRetry(
        IOptions<SmtpSettings> settings,
        ILogger<SmtpMessengerWithRetry> logger,
        int maxRetries = 5,
        double baseDelaySeconds = 2.0)
    {
        _settings = settings.Value;
        _logger = logger;
        _maxRetries = maxRetries;
        _baseDelaySeconds = baseDelaySeconds;
        
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = new InvalidOperationException("No attempts made");

        while (attempt < _maxRetries)
        {
            try
            {
                await SendEmailAsync(to, subject, body, cancellationToken);
                _logger.LogInformation(
                    "[SmtpMessengerWithRetry] Email sent successfully to {To} with subject: {Subject} on attempt {Attempt}",
                    to, subject, attempt + 1);
                return;
            }
            catch (SmtpException smtpEx) when (attempt < _maxRetries - 1)
            {
                attempt++;
                lastException = smtpEx;
                
                // Exponential backoff: delay = baseDelay * 2^attempt
                var delay = TimeSpan.FromSeconds(_baseDelaySeconds * Math.Pow(2, attempt));
                
                _logger.LogWarning(
                    smtpEx,
                    "[SmtpMessengerWithRetry] SMTP error sending email to {To}. Attempt {Attempt}/{MaxRetries}. Retrying in {DelaySeconds}s...",
                    to, attempt, _maxRetries, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (IOException ioEx) when (attempt < _maxRetries - 1)
            {
                attempt++;
                lastException = ioEx;
                
                var delay = TimeSpan.FromSeconds(_baseDelaySeconds * Math.Pow(2, attempt));
                
                _logger.LogWarning(
                    ioEx,
                    "[SmtpMessengerWithRetry] IO error sending email to {To}. Attempt {Attempt}/{MaxRetries}. Retrying in {DelaySeconds}s...",
                    to, attempt, _maxRetries, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex) when (attempt < _maxRetries - 1)
            {
                attempt++;
                lastException = ex;
                
                var delay = TimeSpan.FromSeconds(_baseDelaySeconds * Math.Pow(2, attempt));
                
                _logger.LogWarning(
                    ex,
                    "[SmtpMessengerWithRetry] Unexpected error sending email to {To}. Attempt {Attempt}/{MaxRetries}. Retrying in {DelaySeconds}s...",
                    to, attempt, _maxRetries, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // All retries exhausted
        _logger.LogError(
            lastException,
            "[SmtpMessengerWithRetry] Failed to send email to {To} with subject '{Subject}' after {MaxRetries} attempts",
            to, subject, _maxRetries);

        throw new MailDeliveryException(
            $"Failed to send email to {to} after {_maxRetries} attempts. See inner exception for details.",
            lastException);
    }

    private async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        
        using var client = new SmtpClient(_settings.Host, _settings.Port);
        
        if (!string.IsNullOrEmpty(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }
        
        client.EnableSsl = _settings.EnableSsl;
        client.Timeout = 30000; // 30 seconds timeout

        var from = new MailAddress(_settings.From, _settings.FromName);
        var message = new MailMessage(from, new MailAddress(to))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        _logger.LogDebug(
            "[SmtpMessengerWithRetry] Sending email to {To} via {Host}:{Port} with subject: {Subject}",
            to, _settings.Host, _settings.Port, subject);

        await client.SendMailAsync(message, cancellationToken);
    }
}
