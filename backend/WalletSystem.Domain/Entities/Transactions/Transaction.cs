using WalletSystem.Domain.Entities.Wallets;

namespace WalletSystem.Domain.Entities.Transactions;

public class Transaction : ITransaction
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid WalletId { get; set; }
    public Guid? RelatedWalletId { get; set; }  // For transfers
    public string? Reference { get; set; }     // Unique reference for idempotency
    public bool IsCompleted { get; set; }

    // Auditable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Navigation properties
    public Wallet Wallet { get; set; } = null!;
    public Wallet? RelatedWallet { get; set; }
}
