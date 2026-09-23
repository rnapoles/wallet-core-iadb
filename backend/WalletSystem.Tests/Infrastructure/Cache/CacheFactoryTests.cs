using Enyim.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Infrastructure.Cache;
using WalletSystem.Infrastructure.Cache.Providers;
using Xunit;

namespace WalletSystem.Tests.Infrastructure.Cache;

public class CacheFactoryTests
{
    [Fact]
    public void Create_WithInMemoryType_ReturnsInMemoryCacheProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var serviceProvider = services.BuildServiceProvider();

        var factory = new CacheFactory(serviceProvider);

        // Act
        var provider = factory.Create(CacheProviderType.InMemory);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<InMemoryCacheProvider>(provider);
    }

    [Fact]
    public void Create_WithFileType_ReturnsFileCacheProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var factory = new CacheFactory(serviceProvider);

        // Act
        var provider = factory.Create(CacheProviderType.File);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<FileCacheProvider>(provider);
    }

    [Fact]
    public void Create_WithRedisType_WhenRedisRegistered_ReturnsRedisCacheProvider()
    {
        // Arrange
        var mockConnection = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockConnection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);

        var services = new ServiceCollection();
        services.AddSingleton(mockConnection.Object);
        var serviceProvider = services.BuildServiceProvider();

        var factory = new CacheFactory(serviceProvider);

        // Act
        var provider = factory.Create(CacheProviderType.Redis);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<RedisCacheProvider>(provider);
    }

    [Fact]
    public void Create_WithMemcachedType_WhenMemcachedRegistered_ReturnsMemcachedCacheProvider()
    {
        // Arrange
        var mockClient = new Mock<IMemcachedClient>();

        var services = new ServiceCollection();
        services.AddSingleton(mockClient.Object);
        var serviceProvider = services.BuildServiceProvider();

        var factory = new CacheFactory(serviceProvider);

        // Act
        var provider = factory.Create(CacheProviderType.Memcached);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<MemcachedCacheProvider>(provider);
    }

    [Fact]
    public void Create_WithUnregisteredDependency_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var factory = new CacheFactory(serviceProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.Create(CacheProviderType.Redis));
    }
}
