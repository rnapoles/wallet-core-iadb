using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Transactions;
using WalletSystem.Domain.Entities.Wallets;

namespace WalletSystem.Application.Features.Transactions.Deposit;

public class DepositHandler : IRequestHandler<DepositCommand, TransactionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DepositHandler> _logger;
    private readonly IIdGenerator _idGenerator;

    public DepositHandler(
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<DepositHandler> logger,
        IIdGenerator idGenerator)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _idGenerator = idGenerator;
    }

    public async Task<TransactionResponse> Handle(DepositCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var wallet = await _unitOfWork.Wallets.GetByIdWithLockAsync(request.WalletId, cancellationToken);
            if (wallet == null)
            {
                throw new InvalidOperationException("Wallet not found");
            }

            if (!string.IsNullOrEmpty(request.Reference))
            {
                var existingTransaction = await _unitOfWork.Transactions.ExistsByReferenceAsync(request.Reference, cancellationToken);
                if (existingTransaction)
                {
                    throw new InvalidOperationException("Transaction with this reference already exists");
                }
            }

            var transaction = new Transaction
            {
                Id = _idGenerator.CreateId(),
                Type = TransactionType.Deposit,
                Amount = request.Amount,
                Description = request.Description,
                WalletId = request.WalletId,
                Reference = request.Reference,
                CreatedAt = DateTime.UtcNow,
                IsCompleted = true
            };

            wallet.Balance += request.Amount;
            wallet.LastTransactionAt = DateTime.UtcNow;

            await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
            _unitOfWork.Wallets.Update(wallet);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var user = await _unitOfWork.Users.GetByIdAsync(wallet.UserId, cancellationToken);
            
            var depositCompletedEvent = new DepositCompleted
            {
                TransactionId = transaction!.Id,
                WalletId = wallet.Id,
                Amount = transaction.Amount,
                NewBalance = wallet.Balance,
                Email = user!.Email
            };
            await _publishEndpoint.Publish<DepositCompleted>(depositCompletedEvent, cancellationToken);

            var transactionCreatedEvent = new TransactionCreated
            {
                TransactionId = transaction.Id,
                Type = "Deposit",
                Amount = transaction.Amount,
                WalletId = wallet.Id,
                CreatedAt = transaction.CreatedAt
            };
            await _publishEndpoint.Publish<TransactionCreated>(transactionCreatedEvent, cancellationToken);
            
            // Force save in outbox
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Published DepositCompleted and TransactionCreated events for transaction {TransactionId}", transaction.Id);

            return new TransactionResponse(
                transaction.Id,
                "Deposit",
                transaction.Amount,
                wallet.Balance,
                "Deposit successful");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

