namespace WalletSystem.Demo.Shared.Dtos;

public record UserInfo(Guid Id, string Name, string Password, WalletResponse[] Wallets);
