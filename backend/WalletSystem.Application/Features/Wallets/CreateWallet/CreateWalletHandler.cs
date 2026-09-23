using MediatR;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Domain.Contracts.Persistence;
using WalletSystem.Domain.Entities.Wallets;

namespace WalletSystem.Application.Features.Wallets.CreateWallet;

public class CreateWalletHandler : IRequestHandler<CreateWalletCommand, CreateWalletResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;

    public CreateWalletHandler(IUnitOfWork unitOfWork, IIdGenerator idGenerator)
    {
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
    }

    public async Task<CreateWalletResponse> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var wallet = new Wallet
        {
            Id = _idGenerator.CreateId(),
            Name = request.Name,
            Currency = request.Currency,
            UserId = request.UserId,
            Balance = 0m,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Wallets.AddAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateWalletResponse(wallet.Id, wallet.Name, wallet.Currency, wallet.Balance, "Wallet created successfully");
    }
}
