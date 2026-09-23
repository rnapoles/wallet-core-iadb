using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Features.Transactions.Deposit;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Transactions;
using WalletSystem.Domain.Entities.Wallets;

namespace WalletSystem.Application.Features.Transactions.Withdraw;

public class WithdrawHandler : IRequestHandler<WithdrawCommand, TransactionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WithdrawHandler> _logger;
    private readonly IIdGenerator _idGenerator;

    public WithdrawHandler(
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<WithdrawHandler> logger,
        IIdGenerator idGenerator)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _idGenerator = idGenerator;
    }

    public async Task<TransactionResponse> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var wallet = await _unitOfWork.Wallets.GetByIdWithLockAsync(request.WalletId, cancellationToken);
            if (wallet == null)
            {
                throw new InvalidOperationException("Wallet not found");
            }

            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Withdrawal amount must be positive");
            }

            if (!string.IsNullOrEmpty(request.Reference))
            {
                var existingTransaction = await _unitOfWork.Transactions.GetByReferenceAsync(request.Reference, cancellationToken);
                if (existingTransaction != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return new TransactionResponse(
                        existingTransaction.Id,
                        "Withdrawal",
                        existingTransaction.Amount,
                        wallet.Balance,
                        "Withdrawal already processed (idempotent)"
                    );
                }
            }

            if (wallet.Balance < request.Amount)
            {
                throw new InvalidOperationException("Insufficient funds");
            }

            var transaction = new Transaction
            {
                Type = TransactionType.Withdrawal,
                Amount = request.Amount,
                Description = request.Description,
                Reference = request.Reference ?? _idGenerator.AsString(),
                WalletId = request.WalletId,
                CreatedAt = DateTime.UtcNow,
                IsCompleted = true
            };

            wallet.Balance -= request.Amount;
            wallet.LastTransactionAt = DateTime.UtcNow;

            _unitOfWork.Wallets.Update(wallet);
            await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var withdrawalCompletedEvent = new WithdrawalCompleted
            {
                TransactionId = transaction.Id,
                WalletId = wallet.Id,
                Amount = transaction.Amount,
                NewBalance = wallet.Balance,
                Email = ((Wallet) wallet).User.Email
            };
            await _publishEndpoint.Publish<WithdrawalCompleted>(withdrawalCompletedEvent, cancellationToken);

            var transactionCreatedEvent = new TransactionCreated
            {
                TransactionId = transaction.Id,
                Type = "Withdrawal",
                Amount = transaction.Amount,
                WalletId = wallet.Id,
                CreatedAt = transaction.CreatedAt
            };
            await _publishEndpoint.Publish<TransactionCreated>(transactionCreatedEvent, cancellationToken);

            // Force save in outbox
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Published WithdrawalCompleted and TransactionCreated events for transaction {TransactionId}", transaction.Id);

            return new TransactionResponse(
                transaction.Id,
                "Withdrawal",
                transaction.Amount,
                wallet.Balance,
                "Withdrawal completed successfully"
            );
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

