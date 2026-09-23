namespace WalletSystem.Infrastructure.EventBus.Behaviors;

using System.Diagnostics;
using MediatR;

public sealed class OpenTelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource ActivitySource = new("WalletSystem.Api");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        using var activity = ActivitySource.StartActivity($"MediatR: {requestName}", ActivityKind.Internal);
        
        activity?.SetTag("mediatr.request_type", requestName);

        try
        {
            var response = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            throw; // Re-throw to preserve original stack trace
        }
    }
}