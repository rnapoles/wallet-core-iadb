namespace WalletSystem.Application.Contracts.Services.Id;

/// <summary>
/// Defines a contract for generating unique identifiers.
/// </summary>
public interface IIdGenerator<T>
{
    /// <summary>
    /// Generates a new unique identifier.
    /// </summary>
    /// <returns>A unique identifier of type T.</returns>
    T CreateId();
    
    /// <summary>
    /// Generates a new unique identifier formatted directly as a string.
    /// </summary>
    string AsString() => CreateId()?.ToString() ?? string.Empty;
}