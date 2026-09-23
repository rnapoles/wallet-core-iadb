using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WalletSystem.Infrastructure.EventBus.Behaviors;

public class MetricsPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<MetricsPipelineBehavior<TRequest, TResponse>> _logger;

    public MetricsPipelineBehavior(ILogger<MetricsPipelineBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Execute the next link in the pipeline (or the actual Handler)
            var response = await next();
            
            stopwatch.Stop();

            // Log Success Metric
            _logger.LogInformation(
                "Handled request {RequestName} successfully in {ElapsedMs}ms. Status: {Status}", 
                requestName, 
                stopwatch.ElapsedMilliseconds, 
                "Success");

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log Failure Metric with Exception metadata
            _logger.LogError(
                ex, 
                "Request {RequestName} failed after {ElapsedMs}ms. Status: {Status}. Error: {ErrorType}", 
                requestName, 
                stopwatch.ElapsedMilliseconds, 
                "Failure", 
                ex.GetType().Name);

            throw; // Re-throw to preserve original stack trace
        }
    }
}
