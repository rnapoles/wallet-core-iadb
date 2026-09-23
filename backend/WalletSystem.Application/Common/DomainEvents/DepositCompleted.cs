namespace WalletSystem.Application.Common.DomainEvents;

public record DepositCompleted
{
    public Guid TransactionId { get; init; }
    public Guid WalletId { get; init; }
    public decimal Amount { get; init; }
    public decimal NewBalance { get; init; }
    public string Email { get; init; } =  string.Empty;
}


