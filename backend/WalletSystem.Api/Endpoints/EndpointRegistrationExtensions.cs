using System.Reflection;

namespace WalletSystem.Api.Endpoints;

/// <summary>
/// Clean route registration: discovers every static class in the Endpoints assembly
/// that exposes a <c>MapXxxEndpoints(this IEndpointRouteBuilder)</c> extension method
/// and invokes it, so new endpoint groups are picked up automatically.
/// </summary>
public static class EndpointRegistrationExtensions
{
    /// <summary>
    /// Maps all API endpoints (auth, wallets, transactions, health) in one call.
    /// Example: <c>app.MapAllEndpoints();</c>
    /// </summary>
    public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
    {
        // Root redirect (kept from the MVC era)
        app.MapGet("/", () => Results.Redirect("/swagger/index.html"))
           .ExcludeFromDescription()
           .WithName("RootRedirect");

        var mapMethods = typeof(EndpointRegistrationExtensions).Assembly
            .GetTypes()
            .Where(t => t.IsAbstract && t.IsSealed) // static classes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m =>
                m.Name.StartsWith("Map", StringComparison.Ordinal) &&
                m.Name.EndsWith("Endpoints", StringComparison.Ordinal) &&
                m.Name != nameof(MapAllEndpoints) &&
                m.GetParameters().Length == 1 &&
                typeof(IEndpointRouteBuilder).IsAssignableFrom(m.GetParameters()[0].ParameterType))
            .OrderBy(m => m.DeclaringType!.Name, StringComparer.Ordinal);

        foreach (var method in mapMethods)
        {
            method.Invoke(null, new object[] { app });
        }

        return app;
    }
}
