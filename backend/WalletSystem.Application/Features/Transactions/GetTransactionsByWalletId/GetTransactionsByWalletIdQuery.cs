using MediatR;
using WalletSystem.Application.Common.Dtos;

namespace WalletSystem.Application.Features.Transactions.GetTransactionsByWalletId;

public record GetTransactionsByWalletIdQuery(Guid WalletId) : IRequest<IEnumerable<TransactionDto>>;
