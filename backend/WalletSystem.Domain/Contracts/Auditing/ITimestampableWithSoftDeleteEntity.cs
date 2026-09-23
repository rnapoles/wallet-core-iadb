namespace WalletSystem.Domain.Contracts.Auditing;

public interface ITimestampableWithSoftDeleteEntity : ITimestampableEntity, ISoftDeletableEntity
{
}
