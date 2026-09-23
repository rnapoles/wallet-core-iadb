namespace WalletSystem.Application.Features.Auth.Login;

public record LoginResponse(
    Guid UserId,
    string Email,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);
