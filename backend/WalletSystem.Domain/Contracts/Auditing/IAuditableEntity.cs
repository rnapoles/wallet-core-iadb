namespace WalletSystem.Domain.Contracts.Auditing;

public interface IAuditableEntity : IBlameableWithSoftDeletableEntity, ITimestampableWithSoftDeleteEntity
{
}
