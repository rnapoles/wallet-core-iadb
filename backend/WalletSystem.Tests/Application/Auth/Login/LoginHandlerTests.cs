using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Contracts.Services.Persistence;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Application.Features.Auth.Login;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Users.Persistence;
using WalletSystem.Infrastructure.Persistence;
using Xunit;

namespace WalletSystem.Tests.Application.Auth.Login;

public class LoginHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<IIdGenerator> _mockIdGenerator;
    private readonly LoginCommandHandler _handler;

    public LoginHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockIdGenerator = new Mock<IIdGenerator>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _handler = new LoginCommandHandler(
            unitOfWork: _mockUnitOfWork.Object,
            passwordHasher: _mockPasswordHasher.Object,
            jwtTokenService: _mockJwtTokenService.Object,
            publishEndpoint: _mockPublishEndpoint.Object,
            logger: Mock.Of<ILogger<LoginCommandHandler>>(),
            idGenerator: _mockIdGenerator.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");
        var userId = _mockIdGenerator.Object.CreateId();
        var refreshTokenId = _mockIdGenerator.Object.CreateId();
        var user = new User
        {
            Id = userId,
            Email = command.Email,
            PasswordHash = "hashedPassword",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true
        };
        var expectedToken = "jwt_token_123";
        var refreshTokenEntity = new RefreshToken
        {
            Id = refreshTokenId,
            UserId = user.Id,
            Token = "refresh_token_456",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockIdGenerator.SetupSequence(g => g.CreateId())
            .Returns(userId)
            .Returns(refreshTokenId);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(p => p.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        _mockJwtTokenService.Setup(j => j.GenerateToken(user))
            .Returns(expectedToken);

        _mockJwtTokenService.Setup(j => j.GenerateRefreshToken(user.Id))
            .Returns(refreshTokenEntity);

        _mockJwtTokenService.Setup(j => j.AddRefreshTokenAsync(refreshTokenEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(expectedToken, result.Token);
        Assert.Equal("refresh_token_456", result.RefreshToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "Password123!");

        _mockUserRepository.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidPassword_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword!");
        var userId = _mockIdGenerator.Object.CreateId();
        var user = new User
        {
            Id = userId,
            Email = command.Email,
            PasswordHash = "hashedPassword",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(p => p.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InactiveUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");
        var userId = _mockIdGenerator.Object.CreateId();
        var user = new User
        {
            Id = userId,
            Email = command.Email,
            PasswordHash = "hashedPassword",
            FirstName = "John",
            LastName = "Doe",
            IsActive = false
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(p => p.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
