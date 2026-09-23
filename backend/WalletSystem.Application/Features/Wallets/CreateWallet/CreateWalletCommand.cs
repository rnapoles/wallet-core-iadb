using MediatR;

namespace WalletSystem.Application.Features.Wallets.CreateWallet;

public record CreateWalletCommand(
    Guid UserId,
    string Name,
    string Currency = "USD"
) : IRequest<CreateWalletResponse>;
