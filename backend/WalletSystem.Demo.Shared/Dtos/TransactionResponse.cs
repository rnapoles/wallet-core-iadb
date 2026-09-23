namespace WalletSystem.Demo.Shared.Dtos;

public class TransactionResponse
{
    public Guid TransactionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal NewBalance { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } =  true;
}