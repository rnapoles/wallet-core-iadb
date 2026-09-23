using System.Net.Http.Json;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Commands;

/// <summary>
/// Command to retrieve all wallets for the current user
/// Applied Command Pattern
/// </summary>
public sealed class GetUserWalletsCommand : WalletCommandBase<IReadOnlyList<WalletResponse>>
{
    public GetUserWalletsCommand(HttpClient httpClient, IIdGenerator? idGenerator = null)
        : base(httpClient, null, idGenerator)
    {
    }

    public override async Task<IReadOnlyList<WalletResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync("api/wallets", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<WalletResponse>();
        }

        var wallets = await response.Content.ReadFromJsonAsync<IReadOnlyList<WalletResponse>>(cancellationToken: cancellationToken);
        return wallets ?? Array.Empty<WalletResponse>();
    }
}
