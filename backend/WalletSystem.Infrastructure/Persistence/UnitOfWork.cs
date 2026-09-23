using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Transactions.Persistence;
using WalletSystem.Domain.Entities.Users.Persistence;
using WalletSystem.Domain.Entities.Wallets.Persistence;
using WalletSystem.Infrastructure.Persistence.Repositories;

namespace WalletSystem.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _users;
    private readonly IWalletRepository _wallets;
    private readonly ITransactionRepository _transactions;

    public IUserRepository Users => _users;
    public IWalletRepository Wallets => _wallets;
    public ITransactionRepository Transactions => _transactions;

    public UnitOfWork(ApplicationDbContext context, IUserCacheRepository userCacheRepository)
    {
        _context = context;
        _users = new UserRepository(context);
        _wallets = new WalletRepository(context);
        _transactions = new TransactionRepository(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

