using MediatR;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Application.Features.Wallets.GetWalletsByUserId;

public class GetWalletsByUserIdHandler : IRequestHandler<GetWalletsByUserIdQuery, IEnumerable<WalletDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetWalletsByUserIdHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<WalletDto>> Handle(GetWalletsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new InvalidOperationException("User not authenticated");
        }

        var wallets = await _unitOfWork.Wallets.GetByUserIdAsync(userId.Value, cancellationToken);

        return wallets.Select(w => new WalletDto(
            w.Id,
            w.Name,
            w.Currency,
            w.Balance,
            w.CreatedAt,
            w.LastTransactionAt
        ));
    }
}
