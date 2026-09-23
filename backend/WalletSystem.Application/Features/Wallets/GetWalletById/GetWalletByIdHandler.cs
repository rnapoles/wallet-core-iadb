using MediatR;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Application.Features.Wallets.GetWalletById;

public class GetWalletByIdHandler : IRequestHandler<GetWalletByIdQuery, WalletDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWalletByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WalletDto> Handle(GetWalletByIdQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _unitOfWork.Wallets.GetByIdAsync(request.WalletId, cancellationToken);

        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not found");
        }

        return new WalletDto(
            wallet.Id,
            wallet.Name,
            wallet.Currency,
            wallet.Balance,
            wallet.CreatedAt,
            wallet.LastTransactionAt
        );
    }
}
