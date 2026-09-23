using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Infrastructure.Security.Auth;
using WalletSystem.Infrastructure.Security.Cryptographic;
using WalletSystem.Infrastructure.Security.Identity;

namespace WalletSystem.Api.Settings.Extensions;

public static class SecurityAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        
        // Register Application Services
        services.AddScoped<IJwtTokenService, JwtTokenCacheService>();
        services.AddScoped<IPasswordHasher, Argon2IdPasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }
}
