using System.Text.Json;
using StackExchange.Redis;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Infrastructure.Persistence.Repositories;

public class CacheRepository<T> : ICacheRepository<T> where T : class
{
    private readonly IRepository<T> _repository;
    private readonly IDatabase _redis;
    private readonly string _entityType;

    public CacheRepository(IRepository<T> repository, IConnectionMultiplexer redis)
    {
        _repository = repository;
        _redis = redis.GetDatabase();
        _entityType = typeof(T).Name.ToLowerInvariant();
    }

    private string GetKey(Guid id) => $"{_entityType}:{id}";
    private string GetAllKey() => $"{_entityType}:all";

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GetKey(id);
        var cachedValue = await _redis.StringGetAsync(key);

        if (cachedValue.HasValue)
        {
            return JsonSerializer.Deserialize<T>((string)cachedValue!);
        }

        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await CacheEntityAsync(id, entity);
        }

        return entity;
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var allKey = GetAllKey();
        var cachedValue = await _redis.StringGetAsync(allKey);

        if (cachedValue.HasValue)
        {
            return JsonSerializer.Deserialize<List<T>>((string)cachedValue!) ?? Enumerable.Empty<T>();
        }

        var entities = await _repository.GetAllAsync(cancellationToken);
        await CacheAllAsync(entities);

        return entities;
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _repository.AddAsync(entity, cancellationToken);
        await ClearAllCacheAsync(cancellationToken);
        return result;
    }

    public void Update(T entity)
    {
        _repository.Update(entity);
    }

    public void Delete(T entity)
    {
        _repository.Delete(entity);
    }

    public async Task InvalidateCacheAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GetKey(id);
        await _redis.KeyDeleteAsync(key);
        await _redis.KeyDeleteAsync(GetAllKey());
    }

    public async Task ClearAllCacheAsync(CancellationToken cancellationToken = default)
    {
        var pattern = $"{_entityType}:*";
        var endpoints = _redis.Multiplexer.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            var server = _redis.Multiplexer.GetServer(endpoint);
            var keys = server.Keys(pattern: pattern);

            foreach (var key in keys)
            {
                await _redis.KeyDeleteAsync(key);
            }
        }
    }

    private async Task CacheEntityAsync(Guid id, T entity)
    {
        var key = GetKey(id);
        var json = JsonSerializer.Serialize(entity);
        await _redis.StringSetAsync(key, json, TimeSpan.FromMinutes(30));
    }

    private async Task CacheAllAsync(IEnumerable<T> entities)
    {
        var key = GetAllKey();
        var json = JsonSerializer.Serialize(entities);
        await _redis.StringSetAsync(key, json, TimeSpan.FromMinutes(5));
    }
}

