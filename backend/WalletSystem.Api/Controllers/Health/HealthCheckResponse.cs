namespace WalletSystem.Api.Controllers.Health;

public class HealthCheckResponse
{
    public ServiceHealthStatus OverallStatus { get; set; }
    public Dictionary<string, ServiceHealthResponse> Services { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
