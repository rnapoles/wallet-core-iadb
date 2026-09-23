using WalletSystem.Application.Contracts.Services.Id;

namespace WalletSystem.Demo.Client.Core;

/// <summary>
/// Base class for wallet commands providing common functionality
/// </summary>
public abstract class WalletCommandBase<TResult> : IWalletCommand<TResult>
{
    protected readonly HttpClient HttpClient;
    protected readonly string? Reference;
    protected readonly IIdGenerator? IdGenerator;

    protected WalletCommandBase(HttpClient httpClient, string? reference = null, IIdGenerator? idGenerator = null)
    {
        HttpClient = httpClient;
        Reference = reference;
        IdGenerator = idGenerator;
    }

    public abstract Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a unique reference if not provided
    /// </summary>
    protected string GenerateReference(string prefix)
    {
        if (!string.IsNullOrEmpty(Reference))
            return Reference;

        var id = IdGenerator != null ? IdGenerator.CreateId().ToString("N") : Guid.NewGuid().ToString("N");
        var reference = $"{prefix}-{id}";
        
        // Ensure we don't exceed 40 characters and handle short strings safely
        return reference.Length > 40 
            ? reference[..40].ToUpper()
            : reference.ToUpper();
    }
}
