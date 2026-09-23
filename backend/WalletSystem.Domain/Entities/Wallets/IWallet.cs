using WalletSystem.Domain.Contracts.Auditing;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Domain.Entities.Wallets;

public interface IWallet: IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Currency { get; set; }
    public decimal Balance { get; set; }
    public Guid UserId { get; set; }
    public DateTime? LastTransactionAt { get; set; }

}