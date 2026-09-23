using Microsoft.Extensions.Caching.Memory;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Infrastructure.Cache.Providers;

namespace WalletSystem.Tests.Infrastructure.Cache;

public class InMemoryCacheProviderTests
{
    [Fact]
    public async Task SetAndGetAsync_ShouldStoreAndRetrieveValue()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var provider = new InMemoryCacheProvider(memoryCache);
        var item = new TestModel("Sample", 42);

        // Act
        await provider.SetAsync("test:key1", item);
        var retrieved = await provider.GetAsync<TestModel>("test:key1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Sample", retrieved.Name);
        Assert.Equal(42, retrieved.Value);
    }

    [Fact]
    public async Task ExistsAsync_And_RemoveAsync_ShouldWorkAsExpected()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var provider = new InMemoryCacheProvider(memoryCache);
        var item = new TestModel("Sample", 42);

        // Act & Assert
        await provider.SetAsync("test:key2", item);
        Assert.True(await provider.ExistsAsync("test:key2"));

        await provider.RemoveAsync("test:key2");
        Assert.False(await provider.ExistsAsync("test:key2"));
        Assert.Null(await provider.GetAsync<TestModel>("test:key2"));
    }

    [Fact]
    public async Task RemoveByPrefixAsync_ShouldRemoveOnlyMatchingKeys()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var provider = new InMemoryCacheProvider(memoryCache);

        await provider.SetAsync("prefix:item1", new TestModel("A", 1));
        await provider.SetAsync("prefix:item2", new TestModel("B", 2));
        await provider.SetAsync("other:item3", new TestModel("C", 3));

        // Act
        await provider.RemoveByPrefixAsync("prefix:");

        // Assert
        Assert.Null(await provider.GetAsync<TestModel>("prefix:item1"));
        Assert.Null(await provider.GetAsync<TestModel>("prefix:item2"));
        Assert.NotNull(await provider.GetAsync<TestModel>("other:item3"));
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllKeys()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var provider = new InMemoryCacheProvider(memoryCache);

        await provider.SetAsync("k1", new TestModel("A", 1));
        await provider.SetAsync("k2", new TestModel("B", 2));

        // Act
        await provider.ClearAsync();

        // Assert
        Assert.Null(await provider.GetAsync<TestModel>("k1"));
        Assert.Null(await provider.GetAsync<TestModel>("k2"));
    }
}
