namespace WalletSystem.Api.Settings;

/// <summary>
/// Strongly-typed, read-only view of the runtime environment configuration
/// (database engine, event-bus transport and cache provider).
///
/// Instances must be created AFTER the full configuration pipeline has been
/// applied — i.e. after:
///     builder.Configuration.AddEnvironmentFile();
///     builder.Configuration.AddEnvironmentVariables();
/// so that .env values and environment variables override appsettings.json.
/// The composition root registers it as a singleton via
/// <see cref="Extensions.ApiConfigurationServiceExtensions.AddApiConfiguration(WalletSystem.Api.Settings.Extensions.ApiConfigurationServiceExtensions, Microsoft.Extensions.Configuration.IConfiguration)"/>.
/// </summary>
public sealed class ApiConfiguration
{
    // Recognized configuration values (compared case-insensitively).
    private const string SqliteEngine = "SQLite";
    private const string MySqlEngine = "MySQL";
    private const string InMemoryMessagingMode = "InMemory";
    private const string RabbitMqMessagingMode = "RabbitMQ";
    private const string RedisCacheProvider = "Redis";
    private const string MemoryCacheProvider = "Memory";
    private const string FileCacheProvider = "File";
    private const string MemcachedCacheProvider = "Memcached";

    /// <summary>
    /// Reads every setting exactly once from <paramref name="configuration"/>.
    /// Defaults mirror the fallbacks previously used across the DI extensions:
    /// Database:Engine = SQLite, Messaging:Mode = InMemory, Cache:Provider = Redis.
    /// </summary>
    public ApiConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        DatabaseEngine = configuration.GetValue<string>("Database:Engine") ?? SqliteEngine;
        MessagingMode = configuration.GetValue<string>("Messaging:Mode") ?? InMemoryMessagingMode;
        CacheProvider = configuration.GetValue<string>("Cache:Provider") ?? RedisCacheProvider;

        IsSqliteActive = Matches(DatabaseEngine, SqliteEngine);
        IsMySqlActive = !IsSqliteActive; // any non-SQLite engine is treated as MySQL (existing behavior)

        IsMemoryEventBusActive = Matches(MessagingMode, InMemoryMessagingMode);
        IsRabbitMqEventBusActive = Matches(MessagingMode, RabbitMqMessagingMode);

        IsRedisCacheActive = Matches(CacheProvider, RedisCacheProvider);
        IsInMemoryCacheActive = Matches(CacheProvider, MemoryCacheProvider);
        IsFileCacheActive = Matches(CacheProvider, FileCacheProvider);
        IsMemcachedCacheActive = Matches(CacheProvider, MemcachedCacheProvider);
    }

    // Raw configured values (read-only).
    public string DatabaseEngine { get; }
    public string MessagingMode { get; }
    public string CacheProvider { get; }

    // Database engine flags.
    public bool IsSqliteActive { get; }
    public bool IsMySqlActive { get; }

    // Event bus (MassTransit transport) flags.
    public bool IsMemoryEventBusActive { get; }
    public bool IsRabbitMqEventBusActive { get; }

    // Cache provider flags.
    public bool IsRedisCacheActive { get; }
    public bool IsInMemoryCacheActive { get; }
    public bool IsFileCacheActive { get; }
    public bool IsMemcachedCacheActive { get; }

    /// <summary>Returns the active database engine name ("SQLite" or "MySQL").</summary>
    public string GetActiveDatabase() => IsSqliteActive ? SqliteEngine : MySqlEngine;

    /// <summary>Returns the active event-bus transport name ("InMemory" or "RabbitMQ").</summary>
    public string GetActiveEventBus() => IsRabbitMqEventBusActive ? RabbitMqMessagingMode : InMemoryMessagingMode;

    /// <summary>Returns the active cache provider name.</summary>
    public string GetActiveCache() => CacheProvider;

    private static bool Matches(string actual, string expected) =>
        expected.Equals(actual, StringComparison.OrdinalIgnoreCase);
}
