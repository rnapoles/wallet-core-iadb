namespace WalletSystem.Application.Contracts.Services.Cache;

/// <summary>
/// Identifies which backing store the <c>CacheFactory</c> should create.
/// </summary>
public enum CacheProviderType
{
    /// <summary>Thread-safe in-process dictionary (IMemoryCache).</summary>
    InMemory,

    /// <summary>JSON files persisted to disk.</summary>
    File,

    /// <summary>Memcached via EnyimMemcachedCore.</summary>
    Memcached,

    /// <summary>Redis via StackExchange.Redis.</summary>
    Redis
}
