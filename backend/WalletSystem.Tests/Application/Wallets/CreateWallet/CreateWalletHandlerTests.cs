using Moq;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Features.Wallets.CreateWallet;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Users.Persistence;
using WalletSystem.Domain.Entities.Wallets;
using WalletSystem.Domain.Entities.Wallets.Persistence;

namespace WalletSystem.Tests.Application.Wallets.CreateWallet;

public class CreateWalletHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IWalletRepository> _mockWalletRepository;
    private readonly Mock<IIdGenerator> _mockIdGenerator;
    private readonly CreateWalletHandler _handler;

    public CreateWalletHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockWalletRepository = new Mock<IWalletRepository>();
        _mockIdGenerator = new Mock<IIdGenerator>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.Wallets).Returns(_mockWalletRepository.Object);
        SetupIdGeneratorMock();
        _handler = new CreateWalletHandler(_mockUnitOfWork.Object, _mockIdGenerator.Object);
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
    public async Task Handle_ValidRequest_ShouldCreateWalletSuccessfully()
    {
        // Arrange
        var userId = _mockIdGenerator.Object.CreateId();
        var command = new CreateWalletCommand(userId, "Savings Wallet", "USD");
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hashedPassword",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockWalletRepository.Setup(r => r.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet wallet, CancellationToken ct) => wallet);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Savings Wallet", result.Name);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(0m, result.Balance);
        Assert.Equal("Wallet created successfully", result.Message);
        Assert.NotEqual(Guid.Empty, result.WalletId);

        _mockWalletRepository.Verify(r => r.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = _mockIdGenerator.Object.CreateId();
        var command = new CreateWalletCommand(userId, "Savings Wallet", "USD");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

        _mockWalletRepository.Verify(r => r.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

