using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Domain.Entities.Users.Persistence;

public interface IUserRepository : IRepository<IUser>
{
    Task<IUser?> GetByEmailAsync(string email, CancellationToken ct = default);
}
