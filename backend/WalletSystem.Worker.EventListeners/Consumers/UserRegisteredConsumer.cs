using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Contracts.Services.Notification;
using WalletSystem.Domain.Entities.Users;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class UserRegisteredConsumer : IConsumer<IUserRegistered>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private readonly IMessenger _messenger;

    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger, IMessenger messenger)
    {
        _logger = logger;
        _messenger = messenger;
    }

    public async Task Consume(ConsumeContext<IUserRegistered> context)
    {
        var message = context.Message;
        _logger.LogInformation("User registered: {UserId}, Email: {Email}",
            message.UserId, message.Email);

        // Send welcome email
        var subject = "Welcome to WalletSystem!";
        var body = $@"
            <h1>Welcome to WalletSystem!</h1>
            <p>Dear User,</p>
            <p>Your account has been successfully created.</p>
            <p>Email: {message.Email}</p>
            <p>Registered at: {message.RegisteredAt}</p>
            <p>Thank you for joining us!</p>
        ";

        await _messenger.SendAsync(message.Email, subject, body, context.CancellationToken);

        // Create default wallet
        // Add to CRM system
    }
}


