using System.Linq.Expressions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Contracts.Services.Persistence;
using WalletSystem.Domain.Contracts.Auditing;
using WalletSystem.Domain.Entities.Transactions;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Wallets;
using WalletSystem.Infrastructure.Persistence.Configurations;
using WalletSystem.Infrastructure.Persistence.Interceptors;

namespace WalletSystem.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditingInterceptor auditingInterceptor,
        SoftDeleteInterceptor softDeleteInterceptor) 
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new WalletConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        // Reflectively crawl models and apply Global Query Filters + Indexes
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            
            if (!typeof(ISoftDeletableEntity).IsAssignableFrom(entityType.ClrType)) continue;
            
            // Construct and attach: e => !e.IsDeleted
            var filterExpression = ConvertFilterExpression(entityType.ClrType);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filterExpression);

            // Index the flag field for performance optimization
            modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(ISoftDeletableEntity.IsDeleted));
        }
    }

    private static LambdaExpression ConvertFilterExpression(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletableEntity.IsDeleted));
        var notExpression = Expression.Not(property);
            
        return Expression.Lambda(notExpression, parameter);
    }
}

