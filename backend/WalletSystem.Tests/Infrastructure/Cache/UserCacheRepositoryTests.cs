using Moq;
using WalletSystem.Application.Contracts.Services.Cache;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Users.Persistence;
using WalletSystem.Infrastructure.Cache;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Infrastructure.Persistence.Repositories;

namespace WalletSystem.Tests.Infrastructure.Cache;

public class UserCacheRepositoryTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ICacheProvider> _mockCache;
    private readonly Mock<IIdGenerator> _mockIdGenerator;
    private readonly UserCacheRepository _repository;

    public UserCacheRepositoryTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockCache = new Mock<ICacheProvider>();
        _mockIdGenerator = new Mock<IIdGenerator>();
        SetupIdGeneratorMock();

        _repository = new UserCacheRepository(_mockUserRepository.Object, _mockCache.Object);
    }

    private void SetupIdGeneratorMock()
    {
        var sequentialId = 0;
        _mockIdGenerator.Setup(g => g.CreateId()).Returns(() => 
        {
            var bytes = new byte[16];
            var idValue = Interlocked.Increment(ref sequentialId);
            BitConverter.GetBytes(idValue).CopyTo(bytes, 8);
            return new Guid(bytes);
        });
    }

    [Fact]
    public void Update_ShouldCallRepositoryUpdate_AndInvalidateUserCache()
    {
        // Arrange
        var user = new User
        {
            Id = _mockIdGenerator.Object.CreateId(),
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockCache.Setup(c => c.GetAsync<User>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User?)null);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        // Act
        _repository.Update(user);

        // Assert
        _mockUserRepository.Verify(r => r.Update(user), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync($"user:{user.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:email:john.doe@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Update_WhenEmailChanged_ShouldInvalidateBothOldAndNewEmailKeys()
    {
        // Arrange
        var userId = _mockIdGenerator.Object.CreateId();
        var oldUser = new User
        {
            Id = userId,
            Email = "old.email@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        var updatedUser = new User
        {
            Id = userId,
            Email = "new.email@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockCache.Setup(c => c.GetAsync<User>($"user:{userId}", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(oldUser);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        // Act
        _repository.Update(updatedUser);

        // Assert
        _mockUserRepository.Verify(r => r.Update(updatedUser), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync($"user:{userId}", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:email:old.email@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:email:new.email@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Delete_ShouldCallRepositoryDelete_AndInvalidateUserCache()
    {
        // Arrange
        var user = new User
        {
            Id = _mockIdGenerator.Object.CreateId(),
            Email = "jane.doe@example.com",
            FirstName = "Jane",
            LastName = "Doe"
        };

        _mockCache.Setup(c => c.GetAsync<User>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User?)null);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        // Act
        _repository.Delete(user);

        // Assert
        _mockUserRepository.Verify(r => r.Delete(user), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync($"user:{user.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:email:jane.doe@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheHit_ShouldReturnCachedUserWithoutHittingRepository()
    {
        // Arrange
        var userId = _mockIdGenerator.Object.CreateId();
        var cachedUser = new User
        {
            Id = userId,
            Email = "cached@example.com",
            FirstName = "Cached",
            LastName = "User"
        };

        _mockCache.Setup(c => c.GetAsync<User>($"user:{userId}", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cachedUser);

        // Act
        var result = await _repository.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);
        Assert.Equal("cached@example.com", result.Email);
        _mockUserRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMiss_ShouldCallRepositoryAndCacheResult()
    {
        // Arrange
        var userId = _mockIdGenerator.Object.CreateId();
        var dbUser = new User
        {
            Id = userId,
            Email = "db@example.com",
            FirstName = "Db",
            LastName = "User"
        };

        _mockCache.Setup(c => c.GetAsync<User>($"user:{userId}", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User?)null);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(dbUser);

        // Act
        var result = await _repository.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);
        _mockUserRepository.Verify(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.SetAsync($"user:{userId}", It.IsAny<IUser>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.SetAsync($"user:email:{dbUser.Email}", It.IsAny<IUser>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheThrows_ShouldFallbackToRepositoryGracefully()
    {
        // Arrange
        var userId = _mockIdGenerator.Object.CreateId();
        var dbUser = new User
        {
            Id = userId,
            Email = "fallback@example.com"
        };

        _mockCache.Setup(c => c.GetAsync<User>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("Cache service unavailable"));

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(dbUser);

        // Act
        var result = await _repository.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);
        _mockUserRepository.Verify(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldNormalizeEmail_AndCheckCache()
    {
        // Arrange
        var email = "  Test.User@Example.COM  ";
        var normalizedKey = "user:email:test.user@example.com";
        var user = new User
        {
            Id = _mockIdGenerator.Object.CreateId(),
            Email = "test.user@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        _mockCache.Setup(c => c.GetAsync<IUser>(normalizedKey, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.user@example.com", result!.Email);
        _mockCache.Verify(c => c.GetAsync<IUser>(normalizedKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenCacheMiss_ShouldCallRepositoryAndCacheResult()
    {
        // Arrange
        var email = "miss@example.com";
        var dbUser = new User
        {
            Id = _mockIdGenerator.Object.CreateId(),
            Email = email,
            FirstName = "Miss",
            LastName = "User"
        };

        _mockCache.Setup(c => c.GetAsync<User>($"user:email:{email}", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User?)null);

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(dbUser);

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result!.Email);
        _mockUserRepository.Verify(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.SetAsync($"user:{dbUser.Id}", It.IsAny<IUser>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.SetAsync($"user:email:{email}", It.IsAny<IUser>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenCacheHit_ShouldReturnCachedUsersWithoutHittingRepository()
    {
        // Arrange
        var cachedUsers = new List<User>
        {
            new() { Id = _mockIdGenerator.Object.CreateId(), Email = "user1@example.com" },
            new() { Id = _mockIdGenerator.Object.CreateId(), Email = "user2@example.com" }
        };

        _mockCache.Setup(c => c.GetAsync<List<User>>("user:all", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cachedUsers);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockUserRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenCacheMiss_ShouldCallRepositoryAndCacheResult()
    {
        // Arrange
        var dbUsers = new List<User>
        {
            new() { Id = _mockIdGenerator.Object.CreateId(), Email = "user1@example.com" },
            new() { Id = _mockIdGenerator.Object.CreateId(), Email = "user2@example.com" }
        };

        _mockCache.Setup(c => c.GetAsync<List<User>>("user:all", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((List<User>?)null);

        _mockUserRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(dbUsers);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockUserRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.SetAsync("user:all", It.IsAny<List<IUser>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUser_AndOnlyInvalidateAllKey()
    {
        // Arrange
        var user = new User
        {
            Id = _mockIdGenerator.Object.CreateId(),
            Email = "newuser@example.com",
            FirstName = "New",
            LastName = "User"
        };

        _mockUserRepository.Setup(r => r.AddAsync(user, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(user);

        // Act
        var result = await _repository.AddAsync(user);

        // Assert
        Assert.Same(user, result);
        _mockUserRepository.Verify(r => r.AddAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateUserCacheAsync_ShouldDeleteUserKeysAsynchronously()
    {
        // Arrange
        var user = new User
        {
            Id = _mockIdGenerator.Object.CreateId(),
            Email = "invalidate@example.com"
        };

        _mockCache.Setup(c => c.GetAsync<User>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User?)null);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        // Act
        await _repository.InvalidateUserCacheAsync(user);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync($"user:{user.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:email:invalidate@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateCacheAsync_ShouldDeleteUserKeysById()
    {
        // Arrange
        var userId = _mockIdGenerator.Object.CreateId();
        var cachedUser = new User
        {
            Id = userId,
            Email = "cached@example.com"
        };

        _mockCache.Setup(c => c.GetAsync<User>($"user:{userId}", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cachedUser);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        // Act
        await _repository.InvalidateCacheAsync(userId);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync($"user:{userId}", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:email:cached@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearAllCacheAsync_ShouldCallRemoveByPrefix()
    {
        // Arrange
        _mockCache.Setup(c => c.RemoveByPrefixAsync("user:", It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        // Act
        await _repository.ClearAllCacheAsync();

        // Assert
        _mockCache.Verify(c => c.RemoveByPrefixAsync("user:", It.IsAny<CancellationToken>()), Times.Once);
    }
}
