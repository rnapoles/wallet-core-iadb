using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Contracts.Services.Persistence;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Entities.Users;

namespace WalletSystem.Infrastructure.Security.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly IIdGenerator _idGenerator;

    public JwtTokenService(IConfiguration configuration, IApplicationDbContext dbContext, ILogger<JwtTokenService> logger, IIdGenerator idGenerator)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
        _idGenerator = idGenerator;
    }

    public string GenerateToken(IUser user)
    {
        _logger.LogDebug("Generating JWT token for user {UserId}", user.Id);
        
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        _logger.LogDebug("JWT token generated successfully for user {UserId}, expires at {Expiration}", user.Id, token.ValidTo);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public IRefreshToken GenerateRefreshToken(Guid userId)
    {
        _logger.LogDebug("Generating refresh token for user {UserId}", userId);
        
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var refreshTokenDays = int.Parse(jwtSettings["RefreshTokenDays"] ?? "7");

        var refreshToken = new RefreshToken
        {
            Id = _idGenerator.CreateId(),
            UserId = userId,
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays)
        };
        
        _logger.LogDebug("Refresh token generated successfully for user {UserId}, expires at {Expiration}", userId, refreshToken.ExpiresAt);
        return refreshToken;
    }

    public async Task<IRefreshToken?> GetActiveRefreshTokenAsync(string token, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Looking up refresh token in database");
        
        IRefreshToken? refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (refreshToken is not { IsActive: true })
        {
            _logger.LogWarning("Refresh token not found or inactive: {Token}", token);
            return null;
        }
        
        _logger.LogDebug("Active refresh token found for token: {Token}", token);
        return refreshToken;
    }

    public async Task RevokeRefreshTokenAsync(IRefreshToken refreshToken, string reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revoking refresh token for user {UserId}. Reason: {Reason}", refreshToken.UserId, reason);
        
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReasonRevoked = reason;

        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Refresh token revoked successfully for user {UserId}", refreshToken.UserId);
    }

    public async Task AddRefreshTokenAsync(IRefreshToken refreshToken, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Adding new refresh token for user {UserId}", refreshToken.UserId);
        
        await _dbContext.RefreshTokens.AddAsync((RefreshToken)refreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Refresh token added successfully for user {UserId}", refreshToken.UserId);
    }
}

