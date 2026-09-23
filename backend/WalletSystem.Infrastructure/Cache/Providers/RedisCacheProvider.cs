using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using WalletSystem.Application.Contracts.Services.Cache;

namespace WalletSystem.Infrastructure.Cache.Providers;

/// <summary>
/// <see cref="ICacheProvider"/> backed by Redis via <c>StackExchange.Redis</c>.
/// Supports prefix scanning and atomic key deletion.
/// </summary>
public sealed class RedisCacheProvider : ICacheProvider
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheProvider>? _logger;

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    public RedisCacheProvider(IConnectionMultiplexer multiplexer, ILogger<RedisCacheProvider>? logger = null)
    {
        _multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
        _db = _multiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.HasValue)
            {
                return JsonSerializer.Deserialize<T>((string)value!);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Redis GET failed for key '{Key}'.", key);
        }

        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, ttl ?? DefaultTtl);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Redis SET failed for key '{Key}'.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Redis REMOVE failed for key '{Key}'.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        try
        {
            var pattern = $"{prefix}*";
            var keys = await ScanKeysAsync(pattern);
            if (keys.Length > 0)
            {
                await _db.KeyDeleteAsync(keys);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Redis prefix-remove failed for prefix '{Prefix}'.", prefix);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Redis EXISTS check failed for key '{Key}'.", key);
            return false;
        }
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        try
        {
            var endpoints = _multiplexer.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _multiplexer.GetServer(endpoint);
                if (server.IsConnected)
                {
                    await server.FlushDatabaseAsync(_db.Database);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Redis FLUSHDB failed.");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<RedisKey[]> ScanKeysAsync(string pattern)
    {
        var allKeys = new List<RedisKey>();
        var endpoints = _multiplexer.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            var server = _multiplexer.GetServer(endpoint);
            if (!server.IsConnected)
            {
                continue;
            }

            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                allKeys.Add(key);
            }
        }

        return [.. allKeys];
    }
}
