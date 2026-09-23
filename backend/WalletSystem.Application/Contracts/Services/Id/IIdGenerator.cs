namespace WalletSystem.Application.Contracts.Services.Id;

/// <summary>
/// A specialized contract for 128-bit GUID ID generation.
/// </summary>
public interface IIdGenerator : IIdGenerator<Guid>
{
    // Inherits Guid CreateId()
    // Inherits string AsString()
}