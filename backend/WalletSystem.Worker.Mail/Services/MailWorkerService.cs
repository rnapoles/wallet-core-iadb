namespace WalletSystem.Worker.Mail.Services;

/// <summary>
/// Background service that keeps the mail worker running.
/// The actual work is done by MassTransit consumers.
/// </summary>
public class MailWorkerService : BackgroundService
{
    private readonly ILogger<MailWorkerService> _logger;

    public MailWorkerService(ILogger<MailWorkerService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Mail Worker Service started at {Time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Mail Worker Service is running...");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Mail Worker Service stopped at {Time}", DateTimeOffset.Now);
    }
}
