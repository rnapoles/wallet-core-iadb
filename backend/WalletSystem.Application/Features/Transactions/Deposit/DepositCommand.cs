using MediatR;
using WalletSystem.Application.Common.Dtos;

namespace WalletSystem.Application.Features.Transactions.Deposit;

public record DepositCommand(
    Guid WalletId,
    decimal Amount,
    string? Description = null,
    string? Reference = null
) : IRequest<TransactionResponse>;
