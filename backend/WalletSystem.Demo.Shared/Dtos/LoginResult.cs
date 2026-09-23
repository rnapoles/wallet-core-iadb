namespace WalletSystem.Demo.Shared.Dtos;

public record LoginResult(
    string Token,
    string RefreshToken,
    UserInfo User
);
