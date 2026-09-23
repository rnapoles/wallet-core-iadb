using WalletSystem.Api.Settings;

namespace WalletSystem.Api.Settings.Extensions;

/// <summary>
/// Composition-root helpers for <see cref="ApiConfiguration"/>.
/// </summary>
public static class ApiConfigurationServiceExtensions
{
    /// <summary>
    /// Registers <see cref="ApiConfiguration"/> as a singleton snapshot of the
    /// environment configuration. Call this AFTER
    ///     builder.Configuration.AddEnvironmentFile();
    ///     builder.Configuration.AddEnvironmentVariables();
    /// so .env values and environment variables take precedence over appsettings.json.
    /// </summary>
    public static IServiceCollection AddApiConfiguration(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        // Snapshot is created once, at startup, from the fully-built configuration.
        var apiConfiguration = new ApiConfiguration(configuration);

        builder.Services.AddSingleton(apiConfiguration);

        return builder.Services;
    }
}
