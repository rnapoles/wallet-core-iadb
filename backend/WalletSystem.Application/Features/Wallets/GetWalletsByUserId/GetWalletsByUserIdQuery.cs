using MediatR;
using WalletSystem.Application.Common.Dtos;

namespace WalletSystem.Application.Features.Wallets.GetWalletsByUserId;

public record GetWalletsByUserIdQuery : IRequest<IEnumerable<WalletDto>>;
