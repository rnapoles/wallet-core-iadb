using MediatR;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Application.Features.Transactions.GetTransactionsByWalletId;

public class GetTransactionsByWalletIdHandler : IRequestHandler<GetTransactionsByWalletIdQuery, IEnumerable<TransactionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTransactionsByWalletIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TransactionDto>> Handle(GetTransactionsByWalletIdQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _unitOfWork.Transactions.GetByWalletIdAsync(request.WalletId, cancellationToken);

        return transactions.Select(t => new TransactionDto(
            t.Id,
            t.Type.ToString(),
            t.Amount,
            t.Description,
            t.WalletId,
            t.RelatedWalletId,
            t.Reference,
            t.CreatedAt,
            t.IsCompleted
        ));
    }
}
