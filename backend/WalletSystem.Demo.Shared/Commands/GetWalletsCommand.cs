using System.Net.Http.Json;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Shared.Commands;

/// <summary>
/// Command to get wallets for the authenticated user
/// </summary>
public class GetWalletsCommand : ICommand<IEnumerable<WalletResponse>>
{
    private readonly HttpClient _httpClient;

    public GetWalletsCommand(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<WalletResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.GetAsync("api/wallets", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"GetWallets failed with status {response.StatusCode}: {errorContent}",
                    null,
                    response.StatusCode
                );
            }

            var wallets = await response.Content.ReadFromJsonAsync<List<WalletResponse>>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Failed to deserialize wallets response");

            return wallets;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"GetWallets command failed: {ex.Message}", ex);
        }
    }
}