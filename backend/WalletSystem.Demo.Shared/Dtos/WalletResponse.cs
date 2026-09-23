namespace WalletSystem.Demo.Shared.Dtos;

public record WalletResponse(
    Guid Id,
    string Name,
    string Currency,
    decimal Balance,
    DateTime CreatedAt,
    DateTime? LastTransactionAt
);
