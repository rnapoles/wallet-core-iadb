using MediatR;
using WalletSystem.Application.Common.Dtos;

namespace WalletSystem.Application.Features.Transactions.GetTransactionById;

public record GetTransactionByIdQuery(Guid TransactionId) : IRequest<TransactionDto>;
