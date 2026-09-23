namespace WalletSystem.Application.Features.Users.GetCurrentUser;

public record GetCurrentUserResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName
);
