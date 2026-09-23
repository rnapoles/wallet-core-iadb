namespace WalletSystem.Domain.Entities.Users;

public interface IUserRegistered
{
    Guid UserId { get; }
    string Email { get; }
    DateTime RegisteredAt { get; }
}


