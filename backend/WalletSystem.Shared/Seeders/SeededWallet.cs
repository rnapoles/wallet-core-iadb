namespace WalletSystem.Shared.Seeders;

/// <summary>
/// Represents a seeded wallet belonging to a user
/// </summary>
public sealed class SeededWallet
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public decimal InitialBalance { get; init; }
}
