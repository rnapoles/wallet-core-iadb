namespace WalletSystem.Domain.Contracts.Persistence;

public interface ICacheRepository<T> : IRepository<T> where T : class
{
    Task InvalidateCacheAsync(Guid id, CancellationToken cancellationToken = default);
    Task ClearAllCacheAsync(CancellationToken cancellationToken = default);
}
