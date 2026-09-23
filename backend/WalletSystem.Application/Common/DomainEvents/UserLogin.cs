namespace WalletSystem.Application.Common.DomainEvents;

public record UserLogin
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime LoggedInAt { get; init; }
}


