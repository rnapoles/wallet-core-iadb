using Enyim.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Infrastructure.Cache.Providers;

namespace WalletSystem.Infrastructure.Cache;

/// <summary>
/// Creates <see cref="ICacheProvider"/> instances for the requested backing store.
/// </summary>
/// <remarks>
/// Inject this class (or resolve it from the DI container) to obtain a provider
/// without taking a hard dependency on a specific implementation.
/// </remarks>
public sealed class CacheFactory
{
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory? _loggerFactory;

    public CacheFactory(IServiceProvider services, ILoggerFactory? loggerFactory = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Returns an <see cref="ICacheProvider"/> for the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The cache backend to use.</param>
    /// <param name="fileCacheDirectory">
    /// Optional directory for <see cref="CacheProviderType.File"/>.
    /// Falls back to <c>{TempPath}/wallet_cache</c> when null.
    /// </param>
    /// <exception cref="NotSupportedException">Thrown for unknown provider types.</exception>
    public ICacheProvider Create(CacheProviderType type, string? fileCacheDirectory = null)
        => type switch
        {
            CacheProviderType.InMemory => CreateInMemory(),
            CacheProviderType.File => CreateFile(fileCacheDirectory),
            CacheProviderType.Memcached => CreateMemcached(),
            CacheProviderType.Redis => CreateRedis(),
            _ => throw new NotSupportedException($"Cache provider '{type}' is not supported.")
        };

    // ──────────────────────────────────────────────────────────────────────────
    // Private builders
    // ──────────────────────────────────────────────────────────────────────────

    private InMemoryCacheProvider CreateInMemory()
    {
        var memoryCache = GetRequiredService<IMemoryCache>();
        var logger = _loggerFactory?.CreateLogger<InMemoryCacheProvider>();
        return new InMemoryCacheProvider(memoryCache, logger);
    }

    private FileCacheProvider CreateFile(string? directory)
    {
        var logger = _loggerFactory?.CreateLogger<FileCacheProvider>();
        return new FileCacheProvider(directory, logger);
    }

    private MemcachedCacheProvider CreateMemcached()
    {
        var client = GetRequiredService<IMemcachedClient>();
        var logger = _loggerFactory?.CreateLogger<MemcachedCacheProvider>();
        return new MemcachedCacheProvider(client, logger);
    }

    private RedisCacheProvider CreateRedis()
    {
        var multiplexer = GetRequiredService<IConnectionMultiplexer>();
        var logger = _loggerFactory?.CreateLogger<RedisCacheProvider>();
        return new RedisCacheProvider(multiplexer, logger);
    }

    private T GetRequiredService<T>() where T : notnull
    {
        var service = _services.GetService(typeof(T));
        if (service is null)
        {
            throw new InvalidOperationException(
                $"Service '{typeof(T).Name}' is not registered. " +
                $"Ensure it is added to the DI container before using {nameof(CacheFactory)}.");
        }

        return (T)service;
    }
}
