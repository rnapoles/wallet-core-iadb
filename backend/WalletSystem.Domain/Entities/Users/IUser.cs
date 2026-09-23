using WalletSystem.Domain.Contracts.Auditing;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Domain.Entities.Users;

public interface IUser: IAuditableEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool IsActive { get; set; }

}