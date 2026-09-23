using Serilog;
using WalletSystem.Api.Common.Middleware;
using WalletSystem.Api.Settings.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configuration priority (highest to lowest):
// 1. Environment Variables
// 2. .env file
// 3. appsettings.json
builder.Configuration.AddEnvironmentFile();
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
builder.Host.ConfigureSerilog(builder.Configuration);

// CRITICAL: Register HttpContextAccessor so Serilog can read the client IP
builder.Services.AddHttpContextAccessor(); 

// Add services to the container
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddCorsPolicy();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddMessagingServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddTelemetry();


var app = builder.Build();

// Ensure database is created and migrated
await app.InitializeDatabaseAsync();

// Configure the HTTP request pipeline
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(GlobalExceptionHandler.HandleExceptionAsync);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseSerilogRequestLogging(opts =>
{
    // Each request: { requestPath, statusCode, elapsedMs, correlationId, user }
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("CorrelationId", http.TraceIdentifier);
        diag.Set("UserId", http.User?.Identity?.IsAuthenticated == true
            ? http.User.Identity!.Name
            : null);
        diag.Set("ClientIP", http.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger/index.html"));

try
{
    // Dump configuration details before initializing services
    Log.Information("=== WalletSystem Configuration ===");
    Log.Information("Database Engine: {DatabaseEngine}", builder.Configuration.GetValue<string>("Database:Engine") ?? "SQLite");
    Log.Information("Messaging Mode: {MessagingMode}", builder.Configuration.GetValue<string>("Messaging:Mode") ?? "InMemory");
    Log.Information("Cache Provider: {CacheProvider}", builder.Configuration.GetValue<string>("Cache:Provider") ?? "Redis");

    var smtpHost = builder.Configuration.GetValue<string>("SmtpSettings:Host");
    var smtpPort = builder.Configuration.GetValue<int?>("SmtpSettings:Port");
    Log.Information("SMTP Server: {SmtpHost}:{SmtpPort}", smtpHost ?? "not configured", smtpPort ?? 0);

    Log.Information("Event Bus: {EventBus}", builder.Configuration.GetValue<string>("Messaging:Mode") ?? "InMemory");
    Log.Information("=================================");
    
    Log.Information("Starting WalletSystem API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}



