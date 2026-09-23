using MassTransit;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Notification;
using WalletSystem.Shared;

namespace WalletSystem.Worker.Mail.Consumers;

/// <summary>
/// Consumer that handles DepositCompleted events and sends notification emails.
/// Implements retry logic with exponential backoff via SmtpMessengerWithRetry.
/// </summary>
public class DepositCompletedMailConsumer : IConsumer<DepositCompleted>
{
    private readonly IMessenger _messenger;
    private readonly ILogger<DepositCompletedMailConsumer> _logger;

    public DepositCompletedMailConsumer(IMessenger messenger, ILogger<DepositCompletedMailConsumer> logger)
    {
        _messenger = messenger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DepositCompleted> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "[DepositCompletedMailConsumer] Processing deposit completion for TransactionId: {TransactionId}, WalletId: {WalletId}",
            message.TransactionId, message.WalletId);

        // In a real scenario, you would fetch user email from the wallet/user aggregate
        // For now, we use a placeholder - this should be enhanced to fetch actual recipient
        var recipientEmail = message.Email ;
        var subject = $"Deposit Confirmation - {message.Amount:C}";
        var body = $@"
<html>
<body>
    <h2>Deposit Successful</h2>
    <p>Your deposit has been processed successfully.</p>
    <table>
        <tr><td><strong>Transaction ID:</strong></td><td>{message.TransactionId}</td></tr>
        <tr><td><strong>Amount:</strong></td><td>{message.Amount:C}</td></tr>
        <tr><td><strong>New Balance:</strong></td><td>{message.NewBalance:C}</td></tr>
        <tr><td><strong>Time:</strong></td><td>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</td></tr>
    </table>
    <p>Thank you for using WalletSystem!</p>
</body>
</html>";

        try
        {
            await _messenger.SendAsync(recipientEmail, subject, body, context.CancellationToken);
            _logger.LogInformation(
                "[DepositCompletedMailConsumer] Deposit confirmation email sent to {Email} for transaction {TransactionId}",
                recipientEmail, message.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[DepositCompletedMailConsumer] Failed to send deposit confirmation email for transaction {TransactionId}. Message will be moved to DLQ after retries.",
                message.TransactionId);
            throw; // Re-throw to trigger MassTransit retry/DLQ mechanism
        }
    }
}
