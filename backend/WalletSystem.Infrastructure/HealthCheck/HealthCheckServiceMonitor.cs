using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.HealthCheck;

namespace WalletSystem.Infrastructure.HealthCheck;

/// <summary>
/// Monitors all registered health check services.
/// </summary>
public class HealthCheckServiceMonitor : IHealthCheckServiceMonitor
{
    private readonly IEnumerable<IHealthCheckService> _healthCheckServices;

    public HealthCheckServiceMonitor(IEnumerable<IHealthCheckService> healthCheckServices)
    {
        _healthCheckServices = healthCheckServices ?? Enumerable.Empty<IHealthCheckService>();
    }

    public async Task<IEnumerable<ServiceHealthResult>> CheckAllServicesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ServiceHealthResult>();

        foreach (var service in _healthCheckServices)
        {
            try
            {
                var result = await service.CheckHealthAsync(cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                // If a health check throws an unexpected exception, capture it as unhealthy
                results.Add(new ServiceHealthResult
                {
                    ServiceName = service.ServiceName,
                    Status = ServiceHealthStatus.Unhealthy,
                    Description = $"Unexpected error during health check: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                });
            }
        }

        return results;
    }
}
