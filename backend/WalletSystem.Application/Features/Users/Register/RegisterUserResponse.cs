namespace WalletSystem.Application.Features.Users.Register;

public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string Message
);
