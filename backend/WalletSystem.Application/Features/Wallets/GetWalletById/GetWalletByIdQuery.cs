using MediatR;
using WalletSystem.Application.Common.Dtos;

namespace WalletSystem.Application.Features.Wallets.GetWalletById;

public record GetWalletByIdQuery(Guid WalletId) : IRequest<WalletDto>;
