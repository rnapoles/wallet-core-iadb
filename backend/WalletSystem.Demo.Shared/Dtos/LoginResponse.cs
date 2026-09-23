namespace WalletSystem.Demo.Shared.Dtos;

// DTOs matching the Application layer responses for HTTP client usage
public record LoginResponse(
    Guid UserId,
    string Email,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);
