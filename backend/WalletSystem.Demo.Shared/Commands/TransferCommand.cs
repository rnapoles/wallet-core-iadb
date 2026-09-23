using System.Net.Http.Json;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Shared.Commands;

/// <summary>
/// Command to transfer funds between wallets
/// </summary>
public class TransferCommand : ICommand<TransactionResponse>
{
    private readonly HttpClient _httpClient;
    private readonly Guid _fromWalletId;
    private readonly Guid _toWalletId;
    private readonly decimal _amount;
    private readonly string? _description;
    private readonly string? _reference;
    private readonly IIdGenerator? _idGenerator;

    public TransferCommand(HttpClient httpClient, Guid fromWalletId, Guid toWalletId, decimal amount, string? description = null, string? reference = null, IIdGenerator? idGenerator = null)
    {
        _httpClient = httpClient;
        _fromWalletId = fromWalletId;
        _toWalletId = toWalletId;
        _amount = amount;
        _description = description;
        _reference = reference;
        _idGenerator = idGenerator;
    }

    public async Task<TransactionResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_idGenerator == null)
        {
            throw new InvalidOperationException("IIdGenerator is required but was not provided. Please inject IIdGenerator when creating TransferCommand.");
        }

        var requestBody = new
        {
            FromWalletId = _fromWalletId,
            ToWalletId = _toWalletId,
            Amount = _amount,
            Description = _description ?? $"Test transfer of {_amount}",
            Reference = _reference ?? _idGenerator.CreateId().ToString("N")
        };

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.PostAsJsonAsync("api/transactions/transfer", requestBody, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Transfer failed with status {response.StatusCode}: {errorContent}",
                    null,
                    response.StatusCode
                );
            }

            var result = await response.Content.ReadFromJsonAsync<TransactionResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Failed to deserialize transfer response");

            return result;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Transfer command failed: {ex.Message}", ex);
        }
    }
}


