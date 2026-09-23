namespace WalletSystem.Demo.Shared.Commands;

/// <summary>
/// Generic command interface following the Command pattern
/// </summary>
public interface ICommand<TResult>
{
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
