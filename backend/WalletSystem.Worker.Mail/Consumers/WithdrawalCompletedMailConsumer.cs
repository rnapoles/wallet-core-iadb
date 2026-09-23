using MassTransit;
using WalletSystem.Application.Common.DomainEvents;
using WalletSystem.Application.Contracts.Services.Notification;
using WalletSystem.Shared;

namespace WalletSystem.Worker.Mail.Consumers;

/// <summary>
/// Consumer that handles WithdrawalCompleted events and sends notification emails.
/// Implements retry logic with exponential backoff via SmtpMessengerWithRetry.
/// </summary>
public class WithdrawalCompletedMailConsumer : IConsumer<WithdrawalCompleted>
{
    private readonly IMessenger _messenger;
    private readonly ILogger<WithdrawalCompletedMailConsumer> _logger;

    public WithdrawalCompletedMailConsumer(IMessenger messenger, ILogger<WithdrawalCompletedMailConsumer> logger)
    {
        _messenger = messenger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WithdrawalCompleted> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "[WithdrawalCompletedMailConsumer] Processing withdrawal completion for TransactionId: {TransactionId}, WalletId: {WalletId}",
            message.TransactionId, message.WalletId);

        var recipientEmail = message.Email; // TODO: Fetch from user/wallet data
        var subject = $"Withdrawal Confirmation - {message.Amount:C}";
        var body = $@"
<html>
<body>
    <h2>Withdrawal Processed</h2>
    <p>Your withdrawal has been processed successfully.</p>
    <table>
        <tr><td><strong>Transaction ID:</strong></td><td>{message.TransactionId}</td></tr>
        <tr><td><strong>Amount:</strong></td><td>{message.Amount:C}</td></tr>
        <tr><td><strong>Remaining Balance:</strong></td><td>{message.NewBalance:C}</td></tr>
        <tr><td><strong>Time:</strong></td><td>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</td></tr>
    </table>
    <p>Thank you for using WalletSystem!</p>
</body>
</html>";

        try
        {
            await _messenger.SendAsync(recipientEmail, subject, body, context.CancellationToken);
            _logger.LogInformation(
                "[WithdrawalCompletedMailConsumer] Withdrawal confirmation email sent to {Email} for transaction {TransactionId}",
                recipientEmail, message.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[WithdrawalCompletedMailConsumer] Failed to send withdrawal confirmation email for transaction {TransactionId}. Message will be moved to DLQ after retries.",
                message.TransactionId);
            throw;
        }
    }
}
