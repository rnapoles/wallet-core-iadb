namespace WalletSystem.Application.Contracts.Services.HealthCheck;

/// <summary>
/// Result of a health check operation for a specific service.
/// </summary>
public class ServiceHealthResult
{
    public string ServiceName { get; set; } = string.Empty;
    public ServiceHealthStatus Status { get; set; }
    public string? Description { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
