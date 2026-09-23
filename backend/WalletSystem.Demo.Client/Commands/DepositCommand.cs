using System.Net.Http.Json;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Commands;

/// <summary>
/// Command to deposit funds into a wallet
/// Applied Command Pattern
/// </summary>
public sealed class DepositCommand : WalletCommandBase<TransactionResponse>
{
    private readonly Guid _walletId;
    private readonly decimal _amount;
    private readonly string _description;

    public DepositCommand(
        HttpClient httpClient, 
        Guid walletId, 
        decimal amount, 
        string description, 
        string? reference = null,
        IIdGenerator? idGenerator = null)
        : base(httpClient, reference, idGenerator)
    {
        _walletId = walletId;
        _amount = amount;
        _description = description;
    }

    public override async Task<TransactionResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var command = new
        {
            WalletId = _walletId,
            Amount = _amount,
            Description = _description,
            Reference = GenerateReference("DEP")
        };

        var response = await HttpClient.PostAsJsonAsync("api/transactions/deposit", command, cancellationToken);
        await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(errorContent, null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<TransactionResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Deposit response was null");
    }
}
