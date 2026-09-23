using Microsoft.Extensions.Logging;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Users.Persistence;

namespace WalletSystem.Infrastructure.Persistence.Repositories;

public class UserCacheRepository : IUserCacheRepository
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheProvider _cache;
    private readonly ILogger<UserCacheRepository>? _logger;
    private const string EntityType = "user";

    private static readonly TimeSpan UserCacheTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AllUsersCacheTtl = TimeSpan.FromMinutes(5);

    public UserCacheRepository(
        IUserRepository userRepository,
        ICacheProvider cache,
        ILogger<UserCacheRepository>? logger = null)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger;
    }

    private static string GetKey(Guid id) => $"{EntityType}:{id}";
    private static string GetEmailKey(string email) => $"{EntityType}:email:{email.Trim().ToLowerInvariant()}";
    private static string GetAllKey() => $"{EntityType}:all";

    public async Task<IUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GetKey(id);

        try
        {
            var cachedUser = await _cache.GetAsync<User>(key, cancellationToken);
            if (cachedUser != null)
            {
                return cachedUser;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to retrieve user {UserId} from cache. Falling back to repository.", id);
        }

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user != null)
        {
            await TryCacheUserAsync(user, cancellationToken);
        }

        return user;
    }

    public async Task<IEnumerable<IUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var allKey = GetAllKey();

        try
        {
            var cachedUsers = await _cache.GetAsync<List<User>>(allKey, cancellationToken);
            if (cachedUsers != null)
            {
                return cachedUsers;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to retrieve all users from cache. Falling back to repository.");
        }

        var users = await _userRepository.GetAllAsync(cancellationToken);
        await TryCacheAllAsync(users, cancellationToken);

        return users;
    }

    public async Task<IUser> AddAsync(IUser entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var result = await _userRepository.AddAsync(entity, cancellationToken);

        // Invalidate only the collection cache; adding a user does not affect existing individual user caches
        try
        {
            await _cache.RemoveAsync(GetAllKey(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to invalidate collection cache after adding user {UserId}.", entity.Id);
        }

        return result;
    }

    public void Update(IUser entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _userRepository.Update(entity);
        InvalidateUserCache(entity);
    }

    public void Delete(IUser entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _userRepository.Delete(entity);
        InvalidateUserCache(entity);
    }

    public async Task<IUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var emailKey = GetEmailKey(email);

        try
        {
            var cachedUser = await _cache.GetAsync<IUser>(emailKey, cancellationToken);
            if (cachedUser != null)
            {
                return cachedUser;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to retrieve user by email from cache. Falling back to repository.");
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user != null)
        {
            await TryCacheUserAsync(user, cancellationToken);
        }

        return user;
    }

    public async Task InvalidateCacheAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GetKey(id);

        try
        {
            var cachedUser = await _cache.GetAsync<User>(key, cancellationToken);
            if (cachedUser != null && !string.IsNullOrWhiteSpace(cachedUser.Email))
            {
                await _cache.RemoveAsync(GetEmailKey(cachedUser.Email), cancellationToken);
            }

            await _cache.RemoveAsync(key, cancellationToken);
            await _cache.RemoveAsync(GetAllKey(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invalidating cache for user ID {UserId}", id);
        }
    }

    public async Task InvalidateUserCacheAsync(IUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            // Check if user was previously cached with a different email
            var cachedUser = await _cache.GetAsync<User>(GetKey(user.Id), cancellationToken);
            if (cachedUser != null && !string.IsNullOrWhiteSpace(cachedUser.Email) &&
                !string.Equals(cachedUser.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                await _cache.RemoveAsync(GetEmailKey(cachedUser.Email), cancellationToken);
            }

            await _cache.RemoveAsync(GetKey(user.Id), cancellationToken);
            await _cache.RemoveAsync(GetAllKey(), cancellationToken);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _cache.RemoveAsync(GetEmailKey(user.Email), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invalidating cache for user {UserId}", user.Id);
        }
    }

    public async Task ClearAllCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveByPrefixAsync($"{EntityType}:", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing all user cache.");
        }
    }

    private void InvalidateUserCache(IUser entity)
    {
        try
        {
            InvalidateUserCacheAsync(entity).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invalidating cache for user {UserId}", entity.Id);
        }
    }

    private async Task TryCacheUserAsync(IUser user, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetKey(user.Id);
            await _cache.SetAsync(key, user, UserCacheTtl, cancellationToken);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var emailKey = GetEmailKey(user.Email);
                await _cache.SetAsync(emailKey, user, UserCacheTtl, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to cache user {UserId}.", user.Id);
        }
    }

    private async Task TryCacheAllAsync(IEnumerable<IUser> users, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetAllKey();
            var userList = users as List<IUser> ?? [.. users];
            await _cache.SetAsync(key, userList, AllUsersCacheTtl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to cache all users.");
        }
    }
}
