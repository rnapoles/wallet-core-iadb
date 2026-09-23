using WalletSystem.Domain.Contracts.Auditing;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Domain.Entities.Transactions;

public interface ITransaction: IAuditableEntity
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid WalletId { get; set; }
    public Guid? RelatedWalletId { get; set; }  // For transfers
    public string? Reference { get; set; }     // Unique reference for idempotency
    public bool IsCompleted { get; set; }
}