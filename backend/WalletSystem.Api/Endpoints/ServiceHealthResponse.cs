namespace WalletSystem.Api.Endpoints;

public class ServiceHealthResponse
{
    public string Name { get; set; } = string.Empty;
    public ServiceHealthStatus Status { get; set; }
    public string HealthStatus { get; set; } =  string.Empty;
    public string? Description { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
