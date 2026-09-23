using Serilog;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Infrastructure.Persistence.Seeding;

namespace WalletSystem.Api.Settings.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var idGenerator = scope.ServiceProvider.GetService<IIdGenerator>();
        var passwordHasher = scope.ServiceProvider.GetService<IPasswordHasher>();
        
        await dbContext.Database.EnsureCreatedAsync();
        // For production, use migrations: await dbContext.Database.MigrateAsync();

        // Seed database in development mode
        if (app.Environment.IsDevelopment())
        {
            Log.Information("Seeding database with demo data...");
            await DatabaseSeeder.SeedAsync(dbContext, passwordHasher, idGenerator);
            Log.Information("Database seeding completed.");
        }
    }
}

