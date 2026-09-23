using MassTransit;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.HealthCheck;

namespace WalletSystem.Infrastructure.HealthCheck;

/// <summary>
/// Health check service for RabbitMQ.
/// </summary>
public class RabbitMqHealthCheck : IHealthCheckService
{
    private readonly IBusControl _busControl;

    public RabbitMqHealthCheck(IBusControl busControl)
    {
        _busControl = busControl;
    }

    public string ServiceName => "RabbitMQ";

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
            // Check if the bus is started and connected
            if (_busControl == null)
            {
                result.Status = ServiceHealthStatus.Unhealthy;
                result.Description = "RabbitMQ bus is not configured";
                return result;
            }

            // Check bus readiness
            var busReady = await WaitForBusAsync(cancellationToken);

            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;

            if (busReady)
            {
                result.Status = ServiceHealthStatus.Healthy;
                result.Description = "RabbitMQ connection successful";
            }
            else
            {
                result.Status = ServiceHealthStatus.Unhealthy;
                result.Description = "RabbitMQ bus is not ready";
            }
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = ServiceHealthStatus.Degraded;
            result.Description = "RabbitMQ health check timed out";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = ServiceHealthStatus.Unhealthy;
            result.Description = $"RabbitMQ health check failed: {ex.Message}";
        }

        return result;
    }

    private async Task<bool> WaitForBusAsync(CancellationToken cancellationToken)
    {
        // Create a timeout token for the health check
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await Task.Delay(1, cancellationToken);

            // Check if the bus is available and ready
            if (_busControl != null)
            {
                BusHealthResult busHealthResult = _busControl.CheckHealth();
                // If we have IBusControl, we can check its state
                return busHealthResult.Status == BusHealthStatus.Healthy;
            }

            // The MassTransit framework handles the actual connection state
            return true;
        }
        catch
        {
            return false;
        }
    }
}
