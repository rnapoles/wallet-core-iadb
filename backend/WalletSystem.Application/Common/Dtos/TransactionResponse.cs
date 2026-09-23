namespace WalletSystem.Application.Common.Dtos;

public record TransactionResponse(
    Guid TransactionId,
    string Type,
    decimal Amount,
    decimal NewBalance,
    string Message
);
