using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Features.Transactions.Deposit;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Transactions;
using WalletSystem.Domain.Entities.Transactions.Persistence;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Users.Persistence;
using WalletSystem.Domain.Entities.Wallets;
using WalletSystem.Domain.Entities.Wallets.Persistence;

namespace WalletSystem.Tests.Application.Transactions.Deposit;

public class DepositHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IWalletRepository> _mockWalletRepository;
    private readonly Mock<ITransactionRepository> _mockTransactionRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<IIdGenerator> _mockIdGenerator;
    private readonly DepositHandler _handler;

    public DepositHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockWalletRepository = new Mock<IWalletRepository>();
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockIdGenerator = new Mock<IIdGenerator>();

        _mockUnitOfWork.Setup(u => u.Wallets).Returns(_mockWalletRepository.Object);
        _mockUnitOfWork.Setup(u => u.Transactions).Returns(_mockTransactionRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockIdGenerator.Setup(g => g.CreateId()).Returns(_mockIdGenerator.Object.CreateId());
        _handler = new DepositHandler(_mockUnitOfWork.Object, _mockPublishEndpoint.Object, Mock.Of<ILogger<DepositHandler>>(), _mockIdGenerator.Object);
    }

    [Fact]
    public async Task Handle_ValidDeposit_ShouldUpdateBalanceAndCreateTransaction()
    {
        // Arrange
        var walletId = _mockIdGenerator.Object.CreateId();
        var command = new DepositCommand(walletId, 100m, "Test deposit", "REF001");
        var wallet = new Wallet
        {
            Id = walletId,
            Name = "Test Wallet",
            Currency = "USD",
            Balance = 500m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockTransactionRepository.Setup(r => r.ExistsByReferenceAsync(command.Reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken ct) => t);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockUser = new User
        {
            Id = wallet.UserId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        _mockUserRepository.Setup(r => r.GetByIdAsync(wallet.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Deposit", result.Type);
        Assert.Equal(100m, result.Amount);
        Assert.Equal(600m, result.NewBalance);
        Assert.Equal("Deposit successful", result.Message);
        Assert.Equal(600m, wallet.Balance);

        _mockTransactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<DepositCompleted>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WalletNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var walletId = _mockIdGenerator.Object.CreateId();
        var command = new DepositCommand(walletId, 100m, "Test deposit", "REF001");

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateReference_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var walletId = _mockIdGenerator.Object.CreateId();
        var command = new DepositCommand(walletId, 100m, "Test deposit", "REF001");
        var wallet = new Wallet
        {
            Id = walletId,
            Name = "Test Wallet",
            Currency = "USD",
            Balance = 500m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockTransactionRepository.Setup(r => r.ExistsByReferenceAsync(command.Reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}


