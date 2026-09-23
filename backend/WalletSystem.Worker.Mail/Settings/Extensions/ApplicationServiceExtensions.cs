using WalletSystem.Application.Contracts.Services.Notification;
using WalletSystem.Worker.Mail.Services;

namespace WalletSystem.Worker.Mail.Settings.Extensions;

/// <summary>
/// Extension methods for registering application services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers all application services including messengers and notification services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register IMessenger with retry logic for SMTP communication
        services.AddSingleton<IMessenger, SmtpMessengerWithRetry>();

        return services;
    }
}
