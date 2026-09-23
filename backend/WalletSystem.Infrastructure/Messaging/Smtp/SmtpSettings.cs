namespace WalletSystem.Infrastructure.Messaging.Smtp;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = false;
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

