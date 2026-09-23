using System.Net.Http.Json;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Commands;

/// <summary>
/// Command to retrieve wallet information by ID
/// Applied Command Pattern
/// </summary>
public sealed class GetWalletByIdCommand : WalletCommandBase<WalletResponse>
{
    private readonly Guid _walletId;

    public GetWalletByIdCommand(HttpClient httpClient, Guid walletId, IIdGenerator? idGenerator = null)
        : base(httpClient, null, idGenerator)
    {
        _walletId = walletId;
    }

    public override async Task<WalletResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync($"api/wallets/{_walletId:D}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(errorContent, null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<WalletResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Wallet response was null");
    }
}
