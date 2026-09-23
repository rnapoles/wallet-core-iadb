namespace WalletSystem.Application.Common.Dtos;

public record TransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string? Description,
    Guid WalletId,
    Guid? RelatedWalletId,
    string? Reference,
    DateTime CreatedAt,
    bool IsCompleted
);
