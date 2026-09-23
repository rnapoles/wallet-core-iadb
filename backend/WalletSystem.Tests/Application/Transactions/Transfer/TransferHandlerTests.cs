using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Features.Transactions.Transfer;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Transactions;
using WalletSystem.Domain.Entities.Transactions.Persistence;
using WalletSystem.Domain.Entities.Wallets;
using WalletSystem.Domain.Entities.Wallets.Persistence;

namespace WalletSystem.Tests.Application.Transactions.Transfer;

public class TransferHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IWalletRepository> _mockWalletRepository;
    private readonly Mock<ITransactionRepository> _mockTransactionRepository;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<IIdGenerator> _mockIdGenerator;
    private readonly TransferHandler _handler;

    public TransferHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockWalletRepository = new Mock<IWalletRepository>();
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockIdGenerator = new Mock<IIdGenerator>();

        _mockUnitOfWork.Setup(u => u.Wallets).Returns(_mockWalletRepository.Object);
        _mockUnitOfWork.Setup(u => u.Transactions).Returns(_mockTransactionRepository.Object);
        SetupIdGeneratorMock();
        _handler = new TransferHandler(_mockUnitOfWork.Object, _mockPublishEndpoint.Object, Mock.Of<ILogger<TransferHandler>>(), _mockIdGenerator.Object);
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
        
        _mockIdGenerator.Setup(g => g.AsString()).Returns(() => 
        {
            var bytes = new byte[16];
            var idValue = Interlocked.Increment(ref sequentialId);
            BitConverter.GetBytes(idValue).CopyTo(bytes, 8);
            return new Guid(bytes).ToString("N");
        });
    }

    [Fact]
    public async Task Handle_ValidTransfer_ShouldUpdateBothBalancesAndCreateTransactions()
    {
        // Arrange
        var fromWalletId = _mockIdGenerator.Object.CreateId();
        var toWalletId = _mockIdGenerator.Object.CreateId();
        var command = new TransferCommand(fromWalletId, toWalletId, 100m, "Test transfer", "REF003");

        var fromWallet = new Wallet
        {
            Id = fromWalletId,
            Name = "From Wallet",
            Currency = "USD",
            Balance = 500m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        var toWallet = new Wallet
        {
            Id = toWalletId,
            Name = "To Wallet",
            Currency = "USD",
            Balance = 200m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => id == fromWalletId ? fromWallet : toWallet);

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
        Assert.Equal("Transfer", result.Type);
        Assert.Equal(100m, result.Amount);
        Assert.Equal(400m, result.NewBalance); // From wallet balance after transfer
        Assert.Equal("Transfer completed successfully", result.Message);
        Assert.Equal(400m, fromWallet.Balance);
        Assert.Equal(300m, toWallet.Balance);

        _mockWalletRepository.Verify(r => r.Update(It.IsAny<Wallet>()), Times.Exactly(2));
        _mockTransactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<TransferCompleted>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SameWalletTransfer_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var walletId = _mockIdGenerator.Object.CreateId();
        var command = new TransferCommand(walletId, walletId, 100m, "Test transfer", "REF003");
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

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InsufficientFunds_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fromWalletId = _mockIdGenerator.Object.CreateId();
        var toWalletId = _mockIdGenerator.Object.CreateId();
        var command = new TransferCommand(fromWalletId, toWalletId, 600m, "Test transfer", "REF003");

        var fromWallet = new Wallet
        {
            Id = fromWalletId,
            Name = "From Wallet",
            Currency = "USD",
            Balance = 500m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        var toWallet = new Wallet
        {
            Id = toWalletId,
            Name = "To Wallet",
            Currency = "USD",
            Balance = 200m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => id == fromWalletId ? fromWallet : toWallet);

        _mockTransactionRepository.Setup(r => r.GetByReferenceAsync(command.Reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CurrencyMismatch_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fromWalletId = _mockIdGenerator.Object.CreateId();
        var toWalletId = _mockIdGenerator.Object.CreateId();
        var command = new TransferCommand(fromWalletId, toWalletId, 100m, "Test transfer", "REF003");

        var fromWallet = new Wallet
        {
            Id = fromWalletId,
            Name = "From Wallet",
            Currency = "USD",
            Balance = 500m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        var toWallet = new Wallet
        {
            Id = toWalletId,
            Name = "To Wallet",
            Currency = "EUR",
            Balance = 200m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => id == fromWalletId ? fromWallet : toWallet);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NegativeAmount_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fromWalletId = _mockIdGenerator.Object.CreateId();
        var toWalletId = _mockIdGenerator.Object.CreateId();
        var command = new TransferCommand(fromWalletId, toWalletId, -100m, "Test transfer", "REF003");

        var fromWallet = new Wallet
        {
            Id = fromWalletId,
            Name = "From Wallet",
            Currency = "USD",
            Balance = 500m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        var toWallet = new Wallet
        {
            Id = toWalletId,
            Name = "To Wallet",
            Currency = "USD",
            Balance = 200m,
            UserId = _mockIdGenerator.Object.CreateId()
        };

        _mockWalletRepository.Setup(r => r.GetByIdWithLockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => id == fromWalletId ? fromWallet : toWallet);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}


