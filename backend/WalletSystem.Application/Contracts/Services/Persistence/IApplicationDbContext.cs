using Microsoft.EntityFrameworkCore;
using WalletSystem.Domain.Entities.Transactions;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Wallets;

namespace WalletSystem.Application.Contracts.Services.Persistence;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<Transaction> Transactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
