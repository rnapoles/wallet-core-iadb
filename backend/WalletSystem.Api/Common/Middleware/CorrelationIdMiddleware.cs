namespace WalletSystem.Api.Common.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-Id";
    public async Task Invoke(HttpContext ctx)
    {
        var id = ctx.Request.Headers.TryGetValue(Header, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : ctx.TraceIdentifier; // ASP.NET sets one by default
        ctx.Response.Headers[Header] = id;
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", id))
        {
            await next(ctx);
        }
    }
}