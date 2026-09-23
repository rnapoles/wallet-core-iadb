using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.HealthCheck;

namespace WalletSystem.Api.Controllers.Health;

[ApiController]
[Route("api/health")]
public class HealthReadyController : ControllerBase
{
    private readonly IHealthCheckServiceMonitor _healthCheckServiceMonitor;

    public HealthReadyController(IHealthCheckServiceMonitor healthCheckServiceMonitor)
    {
        _healthCheckServiceMonitor = healthCheckServiceMonitor;
    }

    /// <summary>
    /// Readiness check - verifies if the application is ready to serve traffic
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Readiness(CancellationToken cancellationToken)
    {
        var results = await _healthCheckServiceMonitor.CheckAllServicesAsync(cancellationToken);
        var dbResult = results.FirstOrDefault(r => r.ServiceName == "Database");

        if (dbResult?.Status == (Application.Contracts.Services.HealthCheck.ServiceHealthStatus)ServiceHealthStatus.Healthy)
        {
            return Ok(new { Status = "Ready", Timestamp = DateTime.UtcNow });
        }

        return StatusCode(503, new { Status = "Not Ready", Reason = dbResult?.Description ?? "Database health check failed", Timestamp = DateTime.UtcNow });
    }
}
