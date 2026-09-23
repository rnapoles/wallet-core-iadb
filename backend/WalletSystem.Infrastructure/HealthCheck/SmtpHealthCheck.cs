using System.Net.Sockets;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.HealthCheck;
using WalletSystem.Infrastructure.Messaging.Smtp;

namespace WalletSystem.Infrastructure.HealthCheck;

/// <summary>
/// Health check service for SMTP.
/// </summary>
public class SmtpHealthCheck : IHealthCheckService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpHealthCheck> _logger;

    public SmtpHealthCheck(IOptions<SmtpSettings> settings, ILogger<SmtpHealthCheck> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public string ServiceName => "SMTP";

    public async Task<ServiceHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var result = new ServiceHealthResult
        {
            ServiceName = ServiceName,
            CheckedAt = DateTime.UtcNow
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Validate SMTP settings are configured
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                result.Status = ServiceHealthStatus.Unhealthy;
                result.Description = "SMTP host is not configured";
                return result;
            }

            // Connect to SMTP server using raw TCP to check for 220 banner
            using var client = new TcpClient();
            
            // Connect with timeout
            var connectTask = client.ConnectAsync(_settings.Host, _settings.Port);
            if (await Task.WhenAny(connectTask, Task.Delay(5000, cancellationToken)) != connectTask)
            {
                result.Status = ServiceHealthStatus.Unhealthy;
                result.Description = "Failed to connect to SMTP server within timeout";
                return result;
            }

            await connectTask; // Propagate any connection exceptions

            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            // Read the initial banner (should start with 220)
            var banner = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(banner) || !banner.StartsWith("220"))
            {
                result.Status = ServiceHealthStatus.Unhealthy;
                result.Description = $"Invalid SMTP banner received: {banner ?? "null"}";
                return result;
            }

            // Send QUIT command to close the connection gracefully
            await writer.WriteLineAsync("QUIT");
            await reader.ReadLineAsync(cancellationToken); // Read the 221 response

            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = ServiceHealthStatus.Healthy;
            result.Description = "SMTP server responded with valid 220 banner";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = ServiceHealthStatus.Unhealthy;
            result.Description = $"SMTP health check failed: {ex.Message}";
            _logger.LogError(ex, "SMTP health check failed for {Host}:{Port}", _settings.Host, _settings.Port);
        }

        return result;
    }
}
