using MassTransit;
using Moq;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Application.Features.Users.Register;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Users.Persistence;

namespace WalletSystem.Tests.Application.Users.Register;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<IIdGenerator> _mockIdGenerator;
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockIdGenerator = new Mock<IIdGenerator>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        SetupIdGeneratorMock();
        _handler = new RegisterUserHandler(_mockUnitOfWork.Object, _mockPasswordHasher.Object, _mockPublishEndpoint.Object, Mock.Of<ILogger<RegisterUserHandler>>(), _mockIdGenerator.Object);
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
    public async Task Handle_UserDoesNotExist_ShouldRegisterUserSuccessfully()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Password123!", "John", "Doe");
        var hashedPassword = "hashedPassword";

        _mockUserRepository.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher.Setup(p => p.HashPassword(command.Password))
            .Returns(hashedPassword);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken ct) => user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("User registered successfully", result.Message);
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal(command.Email, result.Email);

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_UserAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Password123!", "John", "Doe");
        var userId = _mockIdGenerator.Object.CreateId();
        var existingUser = new User
        {
            Id = userId,
            Email = command.Email,
            PasswordHash = "existingHash",
            FirstName = "Existing",
            LastName = "User"
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

