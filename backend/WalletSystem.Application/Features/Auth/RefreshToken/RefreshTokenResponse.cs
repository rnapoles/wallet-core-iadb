namespace WalletSystem.Application.Features.Auth.RefreshToken;

public record RefreshTokenResponse(
    Guid UserId,
    string Email,
    string NewAccessToken,
    string NewRefreshToken,
    DateTime ExpiresAt
);
