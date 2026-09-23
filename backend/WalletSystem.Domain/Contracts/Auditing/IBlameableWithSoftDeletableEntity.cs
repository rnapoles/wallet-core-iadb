namespace WalletSystem.Domain.Contracts.Auditing;

public interface IBlameableWithSoftDeletableEntity : IBlameableEntity, ISoftDeletableEntity
{
}
