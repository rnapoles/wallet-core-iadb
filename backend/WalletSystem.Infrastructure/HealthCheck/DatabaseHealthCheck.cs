using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.HealthCheck;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure.HealthCheck;

/// <summary>
/// Health check service for the database.
/// </summary>
public class DatabaseHealthCheck : IHealthCheckService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string ServiceName => "Database";

    public async Task<ServiceHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var result = new ServiceHealthResult
        {
            ServiceName = ServiceName,
            CheckedAt = DateTime.UtcNow
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Test database connectivity with a simple query
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;

            if (canConnect)
            {
                // Additional check: try to query a simple table
                try
                {
                    // Use a lightweight query to verify the database is responsive
                    //_ = await dbContext.Users.AnyAsync(cancellationToken);
                    _ = await dbContext.Database
                        .SqlQuery<int>($"SELECT 1 AS Value")
                        .SingleAsync(cancellationToken);
                    
                    result.Status = ServiceHealthStatus.Healthy;
                    result.Description = "Database connection successful and responsive";
                }
                catch (Exception ex)
                {
                    result.Status = ServiceHealthStatus.Degraded;
                    result.Description = $"Database connected but query failed: {ex.Message}";
                }
            }
            else
            {
                result.Status = ServiceHealthStatus.Unhealthy;
                result.Description = "Unable to connect to database";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = ServiceHealthStatus.Unhealthy;
            result.Description = $"Database health check failed: {ex.Message}";
        }

        return result;
    }
}
