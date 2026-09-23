using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Domain.Entities.Users.Persistence;

public interface IUserCacheRepository : ICacheRepository<IUser>, IUserRepository
{
    Task InvalidateUserCacheAsync(IUser user, CancellationToken cancellationToken = default);
}
