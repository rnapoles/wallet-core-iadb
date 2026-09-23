namespace WalletSystem.Domain.Contracts.Auditing;

public interface IBlameableEntity
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}
