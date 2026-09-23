using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Features.Transactions.Withdraw;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Transactions;
using WalletSystem.Domain.Entities.Transactions.Persistence;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Wallets;
using WalletSystem.Domain.Entities.Wallets.Persistence;

namespace WalletSystem.Tests.Application.Transactions.Withdraw;

public class WithdrawHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IWalletRepository> _mockWalletRepository;
    private readonly Mock<ITransactionRepository> _mockTransactionRepository;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<IIdGenerator> _mockIdGenerator;
    private readonly WithdrawHandler _handler;

    public WithdrawHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockWalletRepository = new Mock<IWalletRepository>();
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockIdGenerator = new Mock<IIdGenerator>();

        _mockUnitOfWork.Setup(u => u.Wallets).Returns(_mockWalletRepository.Object);
        _mockUnitOfWork.Setup(u => u.Transactions).Returns(_mockTransactionRepository.Object);
        _mockIdGenerator.Setup(g => g.AsString()).Returns(_mockIdGenerator.Object.CreateId().ToString("N"));
        _handler = new WithdrawHandler(_mockUnitOfWork.Object, _mockPublishEndpoint.Object, Mock.Of<ILogger<WithdrawHandler>>(), _mockIdGenerator.Object);
    }

    [Fact]
    public async Task Handle_ValidWithdrawal_ShouldUpdateBalanceAndCreateTransaction()
    {
        // Arrange
        var walletId = _mockIdGenerator.Object.CreateId();
        var userId = _mockIdGenerator.Object.CreateId();
        var command = new WithdrawCommand(walletId, 100m, "Test withdrawal", "REF002");
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var wallet = new Wallet
        {
            Id = walletId,
            Name = "Test Wallet",
            Currency = "USD",
            Balance = 500m,
            UserId = userId,
            User = user
        };

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockTransactionRepository.Setup(r => r.GetByReferenceAsync(command.Reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken ct) => t);

        _mockWalletRepository.Setup(r => r.Update(It.IsAny<Wallet>()))
            .Verifiable();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Withdrawal", result.Type);
        Assert.Equal(100m, result.Amount);
        Assert.Equal(400m, result.NewBalance);
        Assert.Equal("Withdrawal completed successfully", result.Message);
        Assert.Equal(400m, wallet.Balance);

        _mockWalletRepository.Verify(r => r.Update(It.IsAny<Wallet>()), Times.Once);
        _mockTransactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<WithdrawalCompleted>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<TransactionCreated>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientFunds_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var walletId = _mockIdGenerator.Object.CreateId();
        var userId = _mockIdGenerator.Object.CreateId();
        var command = new WithdrawCommand(walletId, 600m, "Test withdrawal", "REF002");
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var wallet = new Wallet
        {
            Id = walletId,
            Name = "Test Wallet",
            Currency = "USD",
            Balance = 500m,
            UserId = userId,
            User = user
        };

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockTransactionRepository.Setup(r => r.GetByReferenceAsync(command.Reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NegativeAmount_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var walletId = _mockIdGenerator.Object.CreateId();
        var userId = _mockIdGenerator.Object.CreateId();
        var command = new WithdrawCommand(walletId, -50m, "Test withdrawal", "REF002");
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var wallet = new Wallet
        {
            Id = walletId,
            Name = "Test Wallet",
            Currency = "USD",
            Balance = 500m,
            UserId = userId,
            User = user
        };

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WalletNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var walletId = _mockIdGenerator.Object.CreateId();
        var command = new WithdrawCommand(walletId, 100m, "Test withdrawal", "REF002");

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}


