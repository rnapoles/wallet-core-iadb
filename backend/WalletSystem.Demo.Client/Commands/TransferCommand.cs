using System.Net.Http.Json;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Commands;

/// <summary>
/// Command to transfer funds between wallets
/// Applied Command Pattern
/// </summary>
public sealed class TransferCommand : WalletCommandBase<TransactionResponse>
{
    private readonly Guid _fromWalletId;
    private readonly Guid _toWalletId;
    private readonly decimal _amount;
    private readonly string _description;

    public TransferCommand(
        HttpClient httpClient,
        Guid fromWalletId,
        Guid toWalletId,
        decimal amount,
        string description,
        string? reference = null,
        IIdGenerator? idGenerator = null)
        : base(httpClient, reference, idGenerator)
    {
        _fromWalletId = fromWalletId;
        _toWalletId = toWalletId;
        _amount = amount;
        _description = description;
    }

    public override async Task<TransactionResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var command = new
        {
            FromWalletId = _fromWalletId,
            ToWalletId = _toWalletId,
            Amount = _amount,
            Description = _description,
            Reference = GenerateReference("TRF")
        };

        var response = await HttpClient.PostAsJsonAsync("api/transactions/transfer", command, cancellationToken);
        await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(errorContent, null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<TransactionResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Transfer response was null");
    }
}
