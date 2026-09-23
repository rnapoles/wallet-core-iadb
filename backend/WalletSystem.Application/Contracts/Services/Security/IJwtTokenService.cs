using WalletSystem.Domain.Entities.Users;

namespace WalletSystem.Application.Contracts.Services.Security;

public interface IJwtTokenService
{
    string GenerateToken(IUser user);
    IRefreshToken GenerateRefreshToken(Guid userId);
    Task<IRefreshToken?> GetActiveRefreshTokenAsync(string token, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(IRefreshToken refreshToken, string reason, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(IRefreshToken refreshToken, CancellationToken cancellationToken);
}
