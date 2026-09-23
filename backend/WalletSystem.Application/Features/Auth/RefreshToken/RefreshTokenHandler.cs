using System.IdentityModel.Tokens.Jwt;
using MediatR;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Application.Features.Auth.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenHandler(
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromAccessToken(request.AccessToken);

        if (userId == null)
        {
            throw new InvalidOperationException("Invalid access token");
        }

        var refreshToken = await _jwtTokenService.GetActiveRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshToken == null)
        {
            throw new InvalidOperationException("Invalid or expired refresh token");
        }

        if (refreshToken.UserId != userId)
        {
            throw new InvalidOperationException("Refresh token does not belong to this user");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new InvalidOperationException("User not found or inactive");
        }

        await _jwtTokenService.RevokeRefreshTokenAsync(refreshToken, "Replaced by new token", cancellationToken);

        var newAccessToken = _jwtTokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var newRefreshTokenEntity = _jwtTokenService.GenerateRefreshToken(user.Id);
        await _jwtTokenService.AddRefreshTokenAsync(newRefreshTokenEntity, cancellationToken);

        return new RefreshTokenResponse(
            user.Id,
            user.Email,
            newAccessToken,
            newRefreshTokenEntity.Token,
            expiresAt
        );
    }

    private Guid? GetUserIdFromAccessToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
