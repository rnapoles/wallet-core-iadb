using MassTransit;
using Microsoft.Extensions.Logging;

namespace WalletSystem.Worker.EventListeners.Consumers.Dlq;

/// <summary>
/// Custom consumer for handling messages moved to the Dead Letter Queue (DLQ).
/// Logs details about failed messages including type and correlation ID.
/// </summary>
public class DlqConsumer : IConsumer<object>
{
    private readonly ILogger<DlqConsumer> _logger;

    public DlqConsumer(ILogger<DlqConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<object> context)
    {
        var messageType = context.Message?.GetType().Name ?? "Unknown";
        var correlationId = context.CorrelationId;

        _logger.LogWarning(
            "Message moved to DLQ: {MessageType} with correlation ID: {CorrelationId}",
            messageType,
            correlationId);

        return Task.CompletedTask;
    }
}
