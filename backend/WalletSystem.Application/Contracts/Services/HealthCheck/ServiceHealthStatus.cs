namespace WalletSystem.Application.Contracts.Services.HealthCheck;

/// <summary>
/// Status values for health check results.
/// </summary>
public enum ServiceHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}
