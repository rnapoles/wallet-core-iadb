namespace WalletSystem.Infrastructure.Cache.Providers;

/// <summary>
/// Cache entry wrapper for file-based cache storage.
/// </summary>
internal sealed class CacheEntry<T>
{
    public T? Value { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }

    public CacheEntry() { }

    public CacheEntry(T value, DateTimeOffset expiresAt)
    {
        Value = value;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired() => DateTimeOffset.UtcNow >= ExpiresAt;
}
