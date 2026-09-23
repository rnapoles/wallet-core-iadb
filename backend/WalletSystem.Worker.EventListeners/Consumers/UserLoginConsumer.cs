using MassTransit;
using Microsoft.Extensions.Logging;
using WalletSystem.Application.Contracts.Services.Notification;
using WalletSystem.Domain.Entities.Users;

namespace WalletSystem.Worker.EventListeners.Consumers;

public class UserLoginConsumer : IConsumer<IUserLogin>
{
    private readonly ILogger<UserLoginConsumer> _logger;
    private readonly IMessenger _messenger;

    public UserLoginConsumer(ILogger<UserLoginConsumer> logger, IMessenger messenger)
    {
        _logger = logger;
        _messenger = messenger;
    }

    public async Task Consume(ConsumeContext<IUserLogin> context)
    {
        var message = context.Message;
        _logger.LogInformation("User logged in: {UserId}, Email: {Email}",
            message.UserId, message.Email);

        // Send login notification email
        var subject = "Login Notification - WalletSystem";
        var body = $@"
            <h1>Login Notification</h1>
            <p>Dear User,</p>
            <p>A login was detected on your account.</p>
            <p>Email: {message.Email}</p>
            <p>Login time: {message.LoggedInAt}</p>
            <p>If this wasn't you, please contact support immediately.</p>
        ";

        await _messenger.SendAsync(message.Email, subject, body, context.CancellationToken);
    }
}


