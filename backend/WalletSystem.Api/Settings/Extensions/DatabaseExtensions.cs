using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Api.Settings.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await dbContext.Database.EnsureCreatedAsync();
        // For production, use migrations: await dbContext.Database.MigrateAsync();
    }
}

