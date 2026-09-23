using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Application.Features.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly IIdGenerator _idGenerator;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IPublishEndpoint publishEndpoint,
        ILogger<LoginCommandHandler> logger,
        IIdGenerator idGenerator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _idGenerator = idGenerator;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException("User account is deactivated");
        }

        var token = _jwtTokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);
        await _jwtTokenService.AddRefreshTokenAsync(refreshToken, cancellationToken);

        var userLoginEvent = new UserLogin
        {
            UserId = user.Id,
            Email = user.Email,
            LoggedInAt = DateTime.UtcNow
        };

        await _publishEndpoint.Publish(userLoginEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Published UserLogin event for user {UserId}", user.Id);

        return new LoginResponse(user.Id, user.Email, token, refreshToken.Token, expiresAt);
    }
}

