using MediatR;

namespace WalletSystem.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string AccessToken
) : IRequest<RefreshTokenResponse>;
