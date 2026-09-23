namespace WalletSystem.Application.Common.DomainEvents;

public record UserRegistered
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
}


