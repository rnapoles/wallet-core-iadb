namespace WalletSystem.Application.Contracts.Services.HealthCheck;

/// <summary>
/// Interface for health check services that monitor specific infrastructure components.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Gets the name of the service being monitored.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Performs a health check on the service and returns the result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<ServiceHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
