namespace WalletSystem.Application.Contracts.Services.HealthCheck;

/// <summary>
/// Interface for monitoring multiple health check services.
/// </summary>
public interface IHealthCheckServiceMonitor
{
    /// <summary>
    /// Performs health checks on all registered services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of health check results for all services.</returns>
    Task<IEnumerable<ServiceHealthResult>> CheckAllServicesAsync(CancellationToken cancellationToken = default);
}
