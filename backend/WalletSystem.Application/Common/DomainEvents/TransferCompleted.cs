namespace WalletSystem.Application.Common.DomainEvents;

public record TransferCompleted
{
    public Guid TransactionId { get; init; }
    public Guid FromWalletId { get; init; }
    public Guid ToWalletId { get; init; }
    public decimal Amount { get; init; }
    public decimal FromWalletNewBalance { get; init; }
    public decimal ToWalletNewBalance { get; init; }
}


