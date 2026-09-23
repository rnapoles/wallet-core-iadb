namespace WalletSystem.Shared.Seeders;

/// <summary>
/// Represents a seeded user with their associated wallets
/// </summary>
public sealed class SeededUser
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public IReadOnlyList<SeededWallet> Wallets { get; init; } = Array.Empty<SeededWallet>();
}
