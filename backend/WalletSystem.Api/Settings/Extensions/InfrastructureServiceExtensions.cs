using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using WalletSystem.Api.Settings;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Application.Contracts.Services.HealthCheck;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Contracts.Services.Persistence;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Users.Persistence;
using WalletSystem.Infrastructure.Cache;
using WalletSystem.Infrastructure.Cache.Providers;
using WalletSystem.Infrastructure.HealthCheck;
using WalletSystem.Infrastructure.Id;
using WalletSystem.Infrastructure.Messaging.Smtp;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Infrastructure.Persistence.Interceptors;
using WalletSystem.Infrastructure.Persistence.Repositories;

namespace WalletSystem.Api.Settings.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ApiConfiguration apiConfiguration)
    {
        ArgumentNullException.ThrowIfNull(apiConfiguration);
        
        // Register using the simplified, non-generic contract interface
        services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();

        // Register Interceptors
        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        // Register Database Context & IApplicationDbContext
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

        var useSqlite = apiConfiguration.IsSqliteActive;
        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (useSqlite)
            {
                
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                                       ?? "Data Source=walletsystem.db";
                options
                    .UseSqlite(connectionString)
                    //.LogTo(Console.WriteLine, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
                    //.EnableSensitiveDataLogging()
                    ;
            }
            else
            {
                var connectionString = configuration.GetConnectionString("MySqlConnection");

                options
                    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                    ;
            }
        });

        // Register Unit of Work and Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserCacheRepository, UserCacheRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Cache Provider based on configuration
        services.AddSingleton<CacheFactory>();
        
        if (apiConfiguration.IsInMemoryCacheActive)
        {
            // Register Memory Cache for health checks and application use
            services.AddMemoryCache();
            services.AddSingleton<ICacheProvider, InMemoryCacheProvider>();
        }
        else if (apiConfiguration.IsFileCacheActive)
        {
            // Register File-based cache for local development
            var cacheDirectory = configuration.GetValue<string>("Cache:FileDirectory");
            services.AddSingleton<ICacheProvider>(sp => 
                new FileCacheProvider(cacheDirectory, sp.GetRequiredService<ILogger<FileCacheProvider>>()));
        }
        else if (apiConfiguration.IsMemcachedCacheActive)
        {
            // Register Memcached client via EnyimMemcachedCore
            services.AddEnyimMemcached(options =>
            {
                var serverList = configuration.GetSection("Cache:Memcached:Servers").Get<List<string>>() 
                    ?? new List<string> { "localhost:11211" };
                options.Servers = serverList.Select(s => 
                {
                    var parts = s.Split(':');
                    return new Enyim.Caching.Configuration.Server 
                    { 
                        Address = parts[0], 
                        Port = parts.Length > 1 ? int.Parse(parts[1]) : 11211 
                    };
                }).ToList();
            });
            services.AddSingleton<ICacheProvider, MemcachedCacheProvider>();
        }
        else
        {
            // Default: Redis
            var redisConnection = configuration.GetValue<string>("Cache:RedisConnection") 
                                  ?? configuration.GetValue<string>("RedisSettings:ConnectionString") 
                                  ?? "redis:6379";

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection));
            
            services.AddSingleton<ICacheProvider, RedisCacheProvider>();
        }
        
        // Register SMTP Settings
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));

        // Register Health Check Services
        services.AddSingleton<IHealthCheckService, DatabaseHealthCheck>();
        services.AddSingleton<IHealthCheckService, RabbitMqHealthCheck>();
        services.AddSingleton<IHealthCheckService, CacheHealthCheck>();
        services.AddSingleton<IHealthCheckService, SmtpHealthCheck>();

        // Register Health Check Monitor
        services.AddSingleton<IHealthCheckServiceMonitor, HealthCheckServiceMonitor>();

        return services;
    }
}

