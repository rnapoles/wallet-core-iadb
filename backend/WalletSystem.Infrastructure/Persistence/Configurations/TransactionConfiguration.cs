using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Entities.Transactions;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.WalletId);
        builder.HasIndex(e => e.Reference).IsUnique();
        builder.Property(e => e.Amount).HasPrecision(18, 4);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Reference).HasMaxLength(100);
        
        builder.HasOne(e => e.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(e => e.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RelatedWallet)
            .WithMany()
            .HasForeignKey(e => e.RelatedWalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
