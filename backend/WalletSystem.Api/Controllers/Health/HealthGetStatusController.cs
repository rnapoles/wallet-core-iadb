using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.HealthCheck;

namespace WalletSystem.Api.Controllers.Health;

[ApiController]
[Route("api/health")]
public class HealthGetStatusController : ControllerBase
{
    private readonly IHealthCheckServiceMonitor _healthCheckServiceMonitor;
    private readonly ILogger<HealthGetStatusController> _logger;

    public HealthGetStatusController(
        IHealthCheckServiceMonitor healthCheckServiceMonitor,
        ILogger<HealthGetStatusController> logger)
    {
        _healthCheckServiceMonitor = healthCheckServiceMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Perform a comprehensive health check of all services
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<HealthCheckResponse>> GetHealthStatus(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performing health check");

        var response = new HealthCheckResponse
        {
            OverallStatus = ServiceHealthStatus.Healthy,
            Timestamp = DateTime.UtcNow
        };

        // Check all services using the monitor
        var results = await _healthCheckServiceMonitor.CheckAllServicesAsync(cancellationToken);

        foreach (var result in results)
        {
            var serviceHealth = new ServiceHealthResponse
            {
                Name = result.ServiceName,
                Status = (ServiceHealthStatus)result.Status,
                HealthStatus = result.Status.ToString(),
                Description = result.Description,
                ResponseTime = result.ResponseTime,
                CheckedAt = result.CheckedAt
            };

            response.Services[result.ServiceName] = serviceHealth;
            _logger.LogDebug("{ServiceName} health: {Status} - {Description}",
                serviceHealth.Name, serviceHealth.Status, serviceHealth.Description);
        }

        // Determine overall status
        response.OverallStatus = CalculateOverallStatus(response.Services);

        _logger.LogInformation("Health check completed with overall status: {OverallStatus}", response.OverallStatus);

        // Return appropriate HTTP status code based on overall health
        return response.OverallStatus switch
        {
            ServiceHealthStatus.Healthy => Ok(response),
            ServiceHealthStatus.Degraded => Ok(response),
            ServiceHealthStatus.Unhealthy => StatusCode(503, response),
            _ => StatusCode(500, response)
        };
    }

    private static ServiceHealthStatus CalculateOverallStatus(Dictionary<string, ServiceHealthResponse> services)
    {
        var hasUnhealthy = false;
        var hasDegraded = false;

        foreach (var service in services.Values)
        {
            switch (service.Status)
            {
                case ServiceHealthStatus.Unhealthy:
                    hasUnhealthy = true;
                    break;
                case ServiceHealthStatus.Degraded:
                    hasDegraded = true;
                    break;
            }
        }

        if (hasUnhealthy)
        {
            return ServiceHealthStatus.Unhealthy;
        }

        if (hasDegraded)
        {
            return ServiceHealthStatus.Degraded;
        }

        return ServiceHealthStatus.Healthy;
    }
}
