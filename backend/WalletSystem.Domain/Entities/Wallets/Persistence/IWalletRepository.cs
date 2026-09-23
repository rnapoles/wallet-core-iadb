using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Domain.Entities.Wallets.Persistence;

public interface IWalletRepository : IRepository<IWallet>
{
    Task<IWallet?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<IWallet>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
