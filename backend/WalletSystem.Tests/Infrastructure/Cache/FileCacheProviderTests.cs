using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Infrastructure.Cache.Providers;

namespace WalletSystem.Tests.Infrastructure.Cache;

public class FileCacheProviderTests : IDisposable
{
    private readonly string _testDirectory;

    public FileCacheProviderTests()
    {
        var bytes = new byte[16];
        var idValue = Environment.TickCount;
        BitConverter.GetBytes(idValue).CopyTo(bytes, 8);
        var testGuid = new Guid(bytes);
        _testDirectory = Path.Combine(Path.GetTempPath(), "wallet_file_cache_tests_" + testGuid);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors during test teardown
            }
        }
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldPersistAndRetrievePayload()
    {
        // Arrange
        var provider = new FileCacheProvider(_testDirectory);
        var payload = new TestPayload("FileStored", 100);

        // Act
        await provider.SetAsync("test:payload", payload);
        var retrieved = await provider.GetAsync<TestPayload>("test:payload");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("FileStored", retrieved.Message);
        Assert.Equal(100, retrieved.Code);
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteFile()
    {
        // Arrange
        var provider = new FileCacheProvider(_testDirectory);
        var payload = new TestPayload("DeleteMe", 1);

        await provider.SetAsync("to_delete", payload);
        Assert.True(await provider.ExistsAsync("to_delete"));

        // Act
        await provider.RemoveAsync("to_delete");

        // Assert
        Assert.False(await provider.ExistsAsync("to_delete"));
        Assert.Null(await provider.GetAsync<TestPayload>("to_delete"));
    }

    [Fact]
    public async Task RemoveByPrefixAsync_ShouldDeleteMatchingPrefixes()
    {
        // Arrange
        var provider = new FileCacheProvider(_testDirectory);

        await provider.SetAsync("prefix:p1", new TestPayload("One", 1));
        await provider.SetAsync("prefix:p2", new TestPayload("Two", 2));
        await provider.SetAsync("other:p3", new TestPayload("Three", 3));

        // Act
        await provider.RemoveByPrefixAsync("prefix:");

        // Assert
        Assert.Null(await provider.GetAsync<TestPayload>("prefix:p1"));
        Assert.Null(await provider.GetAsync<TestPayload>("prefix:p2"));
        Assert.NotNull(await provider.GetAsync<TestPayload>("other:p3"));
    }

    [Fact]
    public async Task ClearAsync_ShouldEmptyAllCacheFiles()
    {
        // Arrange
        var provider = new FileCacheProvider(_testDirectory);

        await provider.SetAsync("file1", new TestPayload("One", 1));
        await provider.SetAsync("file2", new TestPayload("Two", 2));

        // Act
        await provider.ClearAsync();

        // Assert
        Assert.Null(await provider.GetAsync<TestPayload>("file1"));
        Assert.Null(await provider.GetAsync<TestPayload>("file2"));
    }
}
