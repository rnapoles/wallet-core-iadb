namespace WalletSystem.Domain.Contracts.Auditing;

public interface ITimestampableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
