using Microsoft.Extensions.Logging;
using System.Text.Json;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.HealthCheck;
using WalletSystem.Application.Contracts.Services.Id;

namespace WalletSystem.Infrastructure.HealthCheck;

/// <summary>
/// Health check service for the distributed cache.
/// </summary>
public class CacheHealthCheck : IHealthCheckService
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IIdGenerator _idGenerator;

    public CacheHealthCheck(ICacheProvider cacheProvider, IIdGenerator idGenerator)
    {
        _cacheProvider = cacheProvider;
        _idGenerator = idGenerator;
    }

    public string ServiceName => "Cache";

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
            if (_cacheProvider == null)
            {
                result.Status = ServiceHealthStatus.Unhealthy;
                result.Description = "Cache is not configured";
                return result;
            }

            // Test cache connectivity with a simple get/set operation
            var testKey = "health_check_" + _idGenerator.AsString();
            var testValue = "test";

            await _cacheProvider.SetAsync(testKey, testValue, TimeSpan.FromSeconds(10), cancellationToken);

            var retrievedValue = await _cacheProvider.GetAsync<string>(testKey, cancellationToken);

            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;

            if (retrievedValue == testValue)
            {
                // Clean up the test key
                await _cacheProvider.RemoveAsync(testKey, cancellationToken);

                result.Status = ServiceHealthStatus.Healthy;
                result.Description = "Cache connection successful and responsive";
            }
            else
            {
                result.Status = ServiceHealthStatus.Degraded;
                result.Description = "Cache connected but data retrieval failed";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = ServiceHealthStatus.Unhealthy;
            result.Description = $"Cache health check failed: {ex.Message}";
        }

        return result;
    }
}
