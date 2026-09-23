using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Domain.Entities.Transactions.Persistence;

public interface ITransactionRepository : IRepository<ITransaction>
{
    Task<ITransaction?> GetByReferenceAsync(string reference, CancellationToken ct = default);
    Task<bool> ExistsByReferenceAsync(string reference, CancellationToken ct = default);
    Task<IEnumerable<ITransaction>> GetByWalletIdAsync(Guid walletId, CancellationToken ct = default);
}
