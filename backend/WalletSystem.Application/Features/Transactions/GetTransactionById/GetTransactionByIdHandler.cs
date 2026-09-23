using MediatR;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Application.Features.Transactions.GetTransactionById;

public class GetTransactionByIdHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTransactionByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(request.TransactionId, cancellationToken);

        if (transaction == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        return new TransactionDto(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.Amount,
            transaction.Description,
            transaction.WalletId,
            transaction.RelatedWalletId,
            transaction.Reference,
            transaction.CreatedAt,
            transaction.IsCompleted
        );
    }
}
