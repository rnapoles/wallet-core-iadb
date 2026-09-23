using WalletSystem.Application.Contracts.Services.Id;

namespace WalletSystem.Demo.Client.Core;

/// <summary>
/// Command interface for wallet operations
/// Applied Command Pattern - encapsulates requests as objects
/// </summary>
public interface IWalletCommand<TResult>
{
    /// <summary>
    /// Execute the command asynchronously
    /// </summary>
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
