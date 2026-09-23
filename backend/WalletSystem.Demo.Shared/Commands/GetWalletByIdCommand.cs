using System.Net.Http.Json;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Shared.Commands;

/// <summary>
/// Command to get wallet by ID
/// </summary>
public class GetWalletByIdCommand : ICommand<WalletResponse>
{
    private readonly HttpClient _httpClient;
    private readonly Guid _walletId;

    public GetWalletByIdCommand(HttpClient httpClient, Guid walletId)
    {
        _httpClient = httpClient;
        _walletId = walletId;
    }

    public async Task<WalletResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.GetAsync($"api/wallets/{_walletId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"GetWalletById ({_walletId}) failed with status {response.StatusCode}: {errorContent}",
                    null,
                    response.StatusCode
                );
            }

            var wallet = await response.Content.ReadFromJsonAsync<WalletResponse>(cancellationToken: cancellationToken)
                         ?? throw new InvalidOperationException("Failed to deserialize wallet response");

            return wallet;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"GetWalletById command failed for {_walletId}: {ex.Message}", ex);
        }
    }
}


