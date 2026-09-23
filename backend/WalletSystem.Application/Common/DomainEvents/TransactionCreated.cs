namespace WalletSystem.Application.Common.DomainEvents;

public record TransactionCreated
{
    public Guid TransactionId { get; init; }
    public string Type { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public Guid WalletId { get; init; }
    public DateTime CreatedAt { get; init; }
}


