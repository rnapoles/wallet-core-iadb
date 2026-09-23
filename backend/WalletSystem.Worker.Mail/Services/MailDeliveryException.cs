namespace WalletSystem.Worker.Mail.Services;

/// <summary>
/// Exception thrown when email delivery fails after all retry attempts.
/// </summary>
public class MailDeliveryException : Exception
{
    public MailDeliveryException(string message) : base(message) { }
    
    public MailDeliveryException(string message, Exception innerException) 
        : base(message, innerException) { }
}
