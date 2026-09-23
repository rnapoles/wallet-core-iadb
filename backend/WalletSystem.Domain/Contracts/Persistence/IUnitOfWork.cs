using WalletSystem.Domain.Entities.Transactions.Persistence;
using WalletSystem.Domain.Entities.Users.Persistence;
using WalletSystem.Domain.Entities.Wallets.Persistence;

namespace WalletSystem.Domain.Contracts.Persistence;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IWalletRepository Wallets { get; }
    ITransactionRepository Transactions { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
