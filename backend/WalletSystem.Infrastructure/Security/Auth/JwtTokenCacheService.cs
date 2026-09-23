using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Contracts.Services.Persistence;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Entities.Users;

namespace WalletSystem.Infrastructure.Security.Auth;

/// <summary>
/// Implementation of <see cref="IJwtTokenService"/> that uses <see cref="ICacheProvider"/>
/// to store refresh tokens in cache for fast access, while persisting them to the database.
/// </summary>
public class JwtTokenCacheService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<JwtTokenCacheService> _logger;
    private readonly IIdGenerator _idGenerator;

    private const string RefreshTokenCachePrefix = "refresh_token:";

    public JwtTokenCacheService(
        IConfiguration configuration,
        IApplicationDbContext dbContext,
        ICacheProvider cacheProvider,
        ILogger<JwtTokenCacheService> logger,
        IIdGenerator idGenerator)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _cacheProvider = cacheProvider;
        _logger = logger;
        _idGenerator = idGenerator;
        
    }

    public string GenerateToken(IUser user)
    {
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

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public IRefreshToken GenerateRefreshToken(Guid userId)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var refreshTokenDays = int.Parse(jwtSettings["RefreshTokenDays"] ?? "7");

        return new RefreshToken
        {
            Id = _idGenerator.CreateId(),
            UserId = userId,
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays)
        };
    }

    public async Task<IRefreshToken?> GetActiveRefreshTokenAsync(string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to get active refresh token for token: {Token}", token);
        
        // First, try to get from cache
        var cacheKey = $"{RefreshTokenCachePrefix}{token}";
        var cachedRefreshToken = await _cacheProvider.GetAsync<RefreshToken>(cacheKey, cancellationToken);

        if (cachedRefreshToken is not null)
        {
            _logger.LogInformation("Found refresh token in cache for token: {Token}", token);
            // Check if the cached token is active (not revoked and not expired)
            bool isActive = cachedRefreshToken.RevokedAt == null && DateTime.UtcNow < cachedRefreshToken.ExpiresAt;
            return isActive ? cachedRefreshToken : null;
        }

        _logger.LogInformation("Refresh token not found in cache, querying database for token: {Token}", token);
        
        // If not in cache, get from database
        IRefreshToken? refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        // Cache the result if found
        if (refreshToken is not null)
        {
            var ttl = CalculateTtl(refreshToken);
            await _cacheProvider.SetAsync(cacheKey, (RefreshToken)refreshToken, ttl, cancellationToken);
            _logger.LogInformation("Cached refresh token from database with TTL: {TTL}", ttl);
        }
        else
        {
            _logger.LogWarning("Refresh token not found in database: {Token}", token);
        }

        return refreshToken is not { IsActive: true } ? null : refreshToken;
    }

    public async Task RevokeRefreshTokenAsync(IRefreshToken refreshToken, string reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revoking refresh token {TokenId} for reason: {Reason}", refreshToken.Id, reason);
        
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReasonRevoked = reason;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Update cache with revoked status
        var cacheKey = $"{RefreshTokenCachePrefix}{refreshToken.Token}";
        await _cacheProvider.SetAsync(cacheKey, (RefreshToken)refreshToken, TimeSpan.FromHours(1), cancellationToken);
        
        _logger.LogInformation("Successfully revoked refresh token {TokenId}", refreshToken.Id);
    }

    public async Task AddRefreshTokenAsync(IRefreshToken refreshToken, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding new refresh token {TokenId} for user {UserId}", refreshToken.Id, refreshToken.UserId);
        
        await _dbContext.RefreshTokens.AddAsync((RefreshToken)refreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Cache the refresh token for fast subsequent lookups
        var cacheKey = $"{RefreshTokenCachePrefix}{refreshToken.Token}";
        var ttl = CalculateTtl(refreshToken);
        await _cacheProvider.SetAsync(cacheKey, (RefreshToken)refreshToken, ttl, cancellationToken);
        
        _logger.LogInformation("Successfully added and cached refresh token {TokenId} with TTL: {TTL}", refreshToken.Id, ttl);
    }

    /// <summary>
    /// Calculates the time-to-live for caching a refresh token.
    /// The TTL is set to the remaining time until the token expires.
    /// </summary>
    private static TimeSpan? CalculateTtl(IRefreshToken refreshToken)
    {
        var remainingTime = refreshToken.ExpiresAt - DateTime.UtcNow;
        return remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero;
    }

}
