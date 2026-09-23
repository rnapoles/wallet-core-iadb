using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WalletSystem.Application.Contracts.Services.Cache;

namespace WalletSystem.Infrastructure.Cache.Providers;

/// <summary>
/// <see cref="ICacheProvider"/> backed by <see cref="IMemoryCache"/>.
/// Suitable for single-instance deployments or testing.
/// </summary>
public sealed class InMemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryCacheProvider>? _logger;

    // Tracks every key so RemoveByPrefix / Clear can work without scanning.
    private readonly HashSet<string> _keys = [];
    private readonly Lock _keyLock = new();

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    public InMemoryCacheProvider(IMemoryCache cache, ILogger<InMemoryCacheProvider>? logger = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        if (_cache.TryGetValue(key, out string? json) && json is not null)
        {
            try
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json));
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Failed to deserialize cached value for key '{Key}'.", key);
            }
        }

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var json = JsonSerializer.Serialize(value);
        var expiry = ttl ?? DefaultTtl;

        _cache.Set(key, json, expiry);

        lock (_keyLock)
        {
            _keys.Add(key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);

        lock (_keyLock)
        {
            _keys.Remove(key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        string[] matching;
        lock (_keyLock)
        {
            matching = [.. _keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal))];
        }

        foreach (var key in matching)
        {
            _cache.Remove(key);
        }

        lock (_keyLock)
        {
            foreach (var key in matching)
            {
                _keys.Remove(key);
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_cache.TryGetValue(key, out _));

    public Task ClearAsync(CancellationToken ct = default)
    {
        string[] snapshot;
        lock (_keyLock)
        {
            snapshot = [.. _keys];
            _keys.Clear();
        }

        foreach (var key in snapshot)
        {
            _cache.Remove(key);
        }

        return Task.CompletedTask;
    }
}
