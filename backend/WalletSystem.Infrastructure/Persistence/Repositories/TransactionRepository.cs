using Microsoft.EntityFrameworkCore;
using WalletSystem.Domain.Entities.Transactions;
using WalletSystem.Domain.Entities.Transactions.Persistence;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Transaction> _dbSet;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Transaction>();
    }

    public async Task<ITransaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync([id], ct);
    }

    public async Task<ITransaction?> GetByReferenceAsync(string reference, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Reference == reference, ct);
    }

    public async Task<bool> ExistsByReferenceAsync(string reference, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(t => t.Reference == reference, ct);
    }

    public async Task<ITransaction> AddAsync(ITransaction transaction, CancellationToken ct = default)
    {
        await _dbSet.AddAsync((Transaction)transaction, ct);
        return transaction;
    }

    public async Task<IEnumerable<ITransaction>> GetByWalletIdAsync(Guid walletId, CancellationToken ct = default)
    {
        return await _dbSet.Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ITransaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public void Update(ITransaction entity)
    {
        _dbSet.Update((Transaction)entity);
    }

    public void Delete(ITransaction entity)
    {
        _dbSet.Remove((Transaction)entity);
    }
}

