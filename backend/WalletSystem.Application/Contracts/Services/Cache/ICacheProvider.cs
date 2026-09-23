namespace WalletSystem.Application.Contracts.Services.Cache;

/// <summary>
/// Provider-agnostic cache abstraction. Implementations exist for
/// InMemory, File, Memcached, and Redis.
/// </summary>
public interface ICacheProvider
{
    /// <summary>Gets a cached value, or null if missing / expired.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>Stores a value with an optional TTL.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;

    /// <summary>Removes a single key.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Removes every key that starts with <paramref name="prefix"/>.</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

    /// <summary>Returns true when the key exists and has not expired.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>Removes all cached entries managed by this provider.</summary>
    Task ClearAsync(CancellationToken ct = default);
}
