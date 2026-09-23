using Microsoft.EntityFrameworkCore;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Users.Persistence;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<User> _dbSet;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<User>();
    }

    public async Task<IUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, ct);
    }

    public async Task<IUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<IUser> AddAsync(IUser user, CancellationToken ct = default)
    {
        await _dbSet.AddAsync((User)user, ct);

        return user;
    }

    public async Task<IEnumerable<IUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public void Update(IUser entity)
    {
        _dbSet.Update((User)entity);
    }

    public void Delete(IUser entity)
    {
        _dbSet.Remove((User)entity);
    }
}

