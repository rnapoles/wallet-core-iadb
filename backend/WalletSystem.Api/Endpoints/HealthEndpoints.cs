using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.HealthCheck;

namespace WalletSystem.Api.Endpoints;

/// <summary>
/// Health endpoints: /api/health/* (anonymous)
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health")
                       .WithTags("Health")
                       .AllowAnonymous();

        // GET /api/health - Perform a comprehensive health check of all services
        group.MapGet("/", async (IHealthCheckServiceMonitor healthCheckServiceMonitor, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("HealthEndpoints");
            logger.LogInformation("Performing health check");

            var response = new HealthCheckResponse
            {
                OverallStatus = ServiceHealthStatus.Healthy,
                Timestamp = DateTime.UtcNow
            };

            // Check all services using the monitor
            var results = await healthCheckServiceMonitor.CheckAllServicesAsync(cancellationToken);

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
                logger.LogDebug("{ServiceName} health: {Status} - {Description}",
                    serviceHealth.Name, serviceHealth.Status, serviceHealth.Description);
            }

            // Determine overall status
            response.OverallStatus = CalculateOverallStatus(response.Services);

            logger.LogInformation("Health check completed with overall status: {OverallStatus}", response.OverallStatus);

            // Return appropriate HTTP status code based on overall health
            return response.OverallStatus switch
            {
                ServiceHealthStatus.Healthy => Results.Ok(response),
                ServiceHealthStatus.Degraded => Results.Ok(response),
                ServiceHealthStatus.Unhealthy => Results.StatusCode(503),
                _ => Results.StatusCode(500)
            };
        })
        .Produces<HealthCheckResponse>()
        .Produces(503)
        .WithSummary("Perform a comprehensive health check of all services");

        // GET /api/health/live - Quick health check endpoint for load balancers
        group.MapGet("/live", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
        .Produces(StatusCodes.Status200OK)
        .WithSummary("Quick liveness check endpoint for load balancers");

        // GET /api/health/ready - Readiness check (verifies the application is ready to serve traffic)
        group.MapGet("/ready", async (IHealthCheckServiceMonitor healthCheckServiceMonitor, CancellationToken cancellationToken) =>
        {
            var results = await healthCheckServiceMonitor.CheckAllServicesAsync(cancellationToken);
            var dbResult = results.FirstOrDefault(r => r.ServiceName == "Database");

            if (dbResult?.Status == (Application.Contracts.Services.HealthCheck.ServiceHealthStatus)ServiceHealthStatus.Healthy)
            {
                return Results.Ok(new { Status = "Ready", Timestamp = DateTime.UtcNow });
            }

            return Results.Json(
                new { Status = "Not Ready", Reason = dbResult?.Description ?? "Database health check failed", Timestamp = DateTime.UtcNow },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(503)
        .WithSummary("Readiness check - verifies if the application is ready to serve traffic");

        return app;
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
