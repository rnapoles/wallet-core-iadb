namespace WalletSystem.Domain.Entities.Users;

public interface IUserLogin
{
    Guid UserId { get; }
    string Email { get; }
    DateTime LoggedInAt { get; }
}


