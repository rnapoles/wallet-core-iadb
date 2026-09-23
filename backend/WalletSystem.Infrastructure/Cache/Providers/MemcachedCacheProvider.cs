using Enyim.Caching;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WalletSystem.Application.Contracts.Services.Cache;

namespace WalletSystem.Infrastructure.Cache.Providers;

/// <summary>
/// <see cref="ICacheProvider"/> backed by Memcached via <c>EnyimMemcachedCore</c>.
/// Register <see cref="IMemcachedClient"/> in the DI container before using this provider.
/// </summary>
public sealed class MemcachedCacheProvider : ICacheProvider
{
    private readonly IMemcachedClient _client;
    private readonly ILogger<MemcachedCacheProvider>? _logger;

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    public MemcachedCacheProvider(IMemcachedClient client, ILogger<MemcachedCacheProvider>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var result = await _client.GetAsync<string>(SanitizeKey(key));
            if (result.Success && result.Value is not null)
            {
                return JsonSerializer.Deserialize<T>(result.Value);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Memcached GET failed for key '{Key}'.", key);
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
            var expiry = (int)(ttl ?? DefaultTtl).TotalSeconds;
            await _client.SetAsync(SanitizeKey(key), json, expiry);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Memcached SET failed for key '{Key}'.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _client.RemoveAsync(SanitizeKey(key));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Memcached REMOVE failed for key '{Key}'.", key);
        }
    }

    /// <remarks>
    /// Memcached has no native prefix-scan. This method is a best-effort no-op;
    /// use a dedicated key-tracking strategy or switch to Redis for this feature.
    /// </remarks>
    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        _logger?.LogWarning(
            "RemoveByPrefixAsync is not natively supported by Memcached. " +
            "Consider using Redis if prefix invalidation is required.");
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var result = await _client.GetAsync<string>(SanitizeKey(key));
            return result.Success && result.Value is not null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Memcached EXISTS check failed for key '{Key}'.", key);
            return false;
        }
    }

    /// <remarks>
    /// Memcached has no native FLUSHDB per client. This implementation is a no-op.
    /// Use the server-level <c>flush_all</c> command if a full clear is required.
    /// </remarks>
    public Task ClearAsync(CancellationToken ct = default)
    {
        _logger?.LogWarning(
            "ClearAsync is not supported by the EnyimMemcachedCore client. " +
            "Issue a flush_all command on the Memcached server directly.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Memcached keys must be ASCII, no whitespace, and ≤ 250 chars.
    /// Replace spaces with underscores; truncate if needed.
    /// </summary>
    private static string SanitizeKey(string key)
    {
        var sanitized = key.Replace(' ', '_');
        return sanitized.Length > 250 ? sanitized[..250] : sanitized;
    }
}
