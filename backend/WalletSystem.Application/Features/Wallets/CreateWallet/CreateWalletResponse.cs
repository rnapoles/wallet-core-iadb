namespace WalletSystem.Application.Features.Wallets.CreateWallet;

public record CreateWalletResponse(
    Guid WalletId,
    string Name,
    string Currency,
    decimal Balance,
    string Message
);
