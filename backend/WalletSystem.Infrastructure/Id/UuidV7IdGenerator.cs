using WalletSystem.Application.Contracts.Services.Id;

namespace WalletSystem.Infrastructure.Id;

public class UuidV7IdGenerator: IIdGenerator
{
    public Guid CreateId() => Guid.CreateVersion7();
    
    public string AsString() => CreateId().ToString("N"); 
}