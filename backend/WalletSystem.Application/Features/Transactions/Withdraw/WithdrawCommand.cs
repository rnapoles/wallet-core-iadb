using MediatR;
using WalletSystem.Application.Common.Dtos;
using WalletSystem.Application.Features.Transactions.Deposit;

namespace WalletSystem.Application.Features.Transactions.Withdraw;

public record WithdrawCommand(
    Guid WalletId,
    decimal Amount,
    string? Description = null,
    string? Reference = null
) : IRequest<TransactionResponse>;
