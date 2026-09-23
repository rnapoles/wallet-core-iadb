using MediatR;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Transactions.Deposit;

namespace WalletSystem.Application.Features.Transactions.Transfer;

public record TransferCommand(
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string? Description = null,
    string? Reference = null
) : IRequest<TransactionResponse>;
