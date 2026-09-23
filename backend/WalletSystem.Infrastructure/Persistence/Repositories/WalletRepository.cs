using Microsoft.EntityFrameworkCore;
using WalletSystem.Domain.Entities.Wallets;
using WalletSystem.Domain.Entities.Wallets.Persistence;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure.Persistence.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Wallet> _dbSet;

    public WalletRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Wallet>();
    }

    public async Task<IWallet?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync([id], ct);
    }

    /// <summary>
    /// Retrieves a wallet with Pessimistic Locking (FOR UPDATE) when running on MySQL,
    /// falling back to standard tracking for SQLite.
    /// </summary>
    public async Task<IWallet?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default)
    {
        if (_context.Database.IsMySql())
        {
            return await _dbSet
                .FromSqlInterpolated($"SELECT * FROM Wallets WHERE Id = {id} FOR UPDATE")
                .Include(u => u.User)
                .AsTracking()
                .FirstOrDefaultAsync(ct);
        }

        return await _dbSet
            .AsTracking()
            .Include(u => u.User)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<IEnumerable<IWallet>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbSet.Where(w => w.UserId == userId).ToListAsync(ct);
    }

    public async Task<IWallet> AddAsync(IWallet wallet, CancellationToken ct = default)
    {
        await _dbSet.AddAsync((Wallet)wallet, ct);
        return wallet;
    }

    public void Update(IWallet wallet)
    {
        _dbSet.Update((Wallet)wallet);
    }

    public async Task<IEnumerable<IWallet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public void Delete(IWallet entity)
    {
        _dbSet.Remove((Wallet)entity);
    }
}

