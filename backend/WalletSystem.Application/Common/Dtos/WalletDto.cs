namespace WalletSystem.Application.Common.Dtos;

public record WalletDto(
    Guid Id,
    string Name,
    string Currency,
    decimal Balance,
    DateTime CreatedAt,
    DateTime? LastTransactionAt
);
