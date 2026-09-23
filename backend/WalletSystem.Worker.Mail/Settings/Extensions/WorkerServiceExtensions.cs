using WalletSystem.Worker.Mail.Services;

namespace WalletSystem.Worker.Mail.Settings.Extensions;

/// <summary>
/// Extension methods for registering worker services.
/// </summary>
public static class WorkerServiceExtensions
{
    /// <summary>
    /// Registers the MailWorkerService as a hosted background service.
    /// </summary>
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddHostedService<MailWorkerService>();
        return services;
    }
}
