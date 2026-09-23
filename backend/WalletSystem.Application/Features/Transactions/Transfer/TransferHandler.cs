using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Features.Transactions.Deposit;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Transactions;

namespace WalletSystem.Application.Features.Transactions.Transfer;

public class TransferHandler : IRequestHandler<TransferCommand, TransactionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TransferHandler> _logger;
    private readonly IIdGenerator _idGenerator;

    public TransferHandler(
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<TransferHandler> logger,
        IIdGenerator idGenerator)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _idGenerator = idGenerator;
    }

    public async Task<TransactionResponse> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var firstWalletId = request.FromWalletId.CompareTo(request.ToWalletId) < 0
                ? request.FromWalletId : request.ToWalletId;
            var secondWalletId = request.FromWalletId.CompareTo(request.ToWalletId) < 0
                ? request.ToWalletId : request.FromWalletId;

            var firstWallet = await _unitOfWork.Wallets.GetByIdWithLockAsync(firstWalletId, cancellationToken);
            var secondWallet = await _unitOfWork.Wallets.GetByIdWithLockAsync(secondWalletId, cancellationToken);

            var fromWallet = request.FromWalletId == firstWalletId ? firstWallet : secondWallet;
            var toWallet = request.ToWalletId == firstWalletId ? firstWallet : secondWallet;

            if (fromWallet == null)
                throw new InvalidOperationException($"Source wallet not found: ${firstWalletId}");

            if (toWallet == null)
                throw new InvalidOperationException("Destination wallet not found: ${secondWalletId}");

            if (fromWallet.Id == toWallet.Id)
                throw new InvalidOperationException("Cannot transfer to the same wallet");

            if (request.Amount <= 0)
                throw new InvalidOperationException("Transfer amount must be positive");

            if (!string.IsNullOrEmpty(request.Reference))
            {
                var existing = await _unitOfWork.Transactions.GetByReferenceAsync(request.Reference, cancellationToken);
                if (existing != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return new TransactionResponse(existing.Id, "Transfer", existing.Amount, fromWallet.Balance, "Transfer already processed (idempotent)");
                }
            }

            if (fromWallet.Balance < request.Amount)
                throw new InvalidOperationException("Insufficient funds");

            if (fromWallet.Currency != toWallet.Currency)
                throw new InvalidOperationException("Currency mismatch between wallets");

            var reference = request.Reference ?? _idGenerator.AsString();
            var creditReference = reference.Length > 96 ? $"{reference[..96]}-IN" : $"{reference}-IN";

            var debitTransaction = new Transaction
            {
                Type = TransactionType.TransferOut,
                Amount = request.Amount,
                Description = request.Description,
                Reference = reference,
                WalletId = request.FromWalletId,
                RelatedWalletId = request.ToWalletId,
                CreatedAt = DateTime.UtcNow,
                IsCompleted = true
            };

            var creditTransaction = new Transaction
            {
                Type = TransactionType.TransferIn,
                Amount = request.Amount,
                Description = request.Description,
                Reference = creditReference,
                WalletId = request.ToWalletId,
                RelatedWalletId = request.FromWalletId,
                CreatedAt = DateTime.UtcNow,
                IsCompleted = true
            };

            fromWallet.Balance -= request.Amount;
            fromWallet.LastTransactionAt = DateTime.UtcNow;
            toWallet.Balance += request.Amount;
            toWallet.LastTransactionAt = DateTime.UtcNow;

            _unitOfWork.Wallets.Update(fromWallet);
            _unitOfWork.Wallets.Update(toWallet);
            await _unitOfWork.Transactions.AddAsync(debitTransaction, cancellationToken);
            await _unitOfWork.Transactions.AddAsync(creditTransaction, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _publishEndpoint.Publish<TransferCompleted>(new TransferCompleted
            {
                TransactionId = debitTransaction.Id,
                FromWalletId = fromWallet.Id,
                ToWalletId = toWallet.Id,
                Amount = request.Amount,
                FromWalletNewBalance = fromWallet.Balance,
                ToWalletNewBalance = toWallet.Balance
            }, cancellationToken);

            await _publishEndpoint.Publish<TransferCompleted>(new TransactionCreated
            {
                TransactionId = debitTransaction.Id,
                Type = "Transfer",
                Amount = request.Amount,
                WalletId = fromWallet.Id,
                CreatedAt = debitTransaction.CreatedAt
            }, cancellationToken);

            // Force save in outbox
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Published TransferCompleted and TransactionCreated events for {TransactionId}", debitTransaction.Id);

            return new TransactionResponse(debitTransaction.Id, "Transfer", request.Amount, fromWallet.Balance, "Transfer completed successfully");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

