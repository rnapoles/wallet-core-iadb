using Serilog;
using WalletSystem.Api.Common.Middleware;
using WalletSystem.Api.Endpoints;
using WalletSystem.Api.Settings;
using WalletSystem.Api.Settings.Extensions;
using WalletSystem.Shared.Settings.Extensions;

var builder = WebApplication.CreateBuilder(args);
const bool useControllers = false;

// Configuration priority (highest to lowest):
// 1. Environment Variables
// 2. .env file
// 3. appsettings.json
builder.Configuration.AddEnvironmentFile();
builder.Configuration.AddEnvironmentVariables();

// Snapshot environment configuration into a strongly-typed singleton.
// Registered AFTER the .env / environment-variable providers so those values win.
builder.AddApiConfiguration(builder.Configuration);
var apiConfiguration = new ApiConfiguration(builder.Configuration);


// Configure Serilog
builder.Host.ConfigureSerilog(builder.Configuration);

// CRITICAL: Register HttpContextAccessor so Serilog can read the client IP
builder.Services.AddHttpContextAccessor(); 

// Add services to the container
builder.Services.AddJwtAuthentication(builder.Configuration);

// 1. Register the Problem Details services
builder.Services.AddProblemDetails();

if (useControllers)
{
    builder.Services.AddControllers();
} 

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddCorsPolicy();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment, apiConfiguration);
builder.Services.AddMessagingServices(builder.Configuration, apiConfiguration);
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

// 2. Automatically turn non-success status codes (e.g., 404, 400) into Problem Details JSON
app.UseStatusCodePages();

// 3. Automatically convert unhandled exceptions into 500 Internal Server Error Problem Details JSON
app.UseExceptionHandler();

if (useControllers)
{
    app.MapControllers();
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));    
}
else
{
    app.MapAllEndpoints();    
}

try
{
    // Dump configuration details before initializing services
    Log.Information("=== WalletSystem Configuration ===");
    Log.Information("Database Engine: {DatabaseEngine}", apiConfiguration.GetActiveDatabase());
    Log.Information("Messaging Mode: {MessagingMode}", apiConfiguration.GetActiveEventBus());
    Log.Information("Cache Provider: {CacheProvider}", apiConfiguration.GetActiveCache());

    var smtpHost = builder.Configuration.GetValue<string>("SmtpSettings:Host");
    var smtpPort = builder.Configuration.GetValue<int?>("SmtpSettings:Port");
    Log.Information("SMTP Server: {SmtpHost}:{SmtpPort}", smtpHost ?? "not configured", smtpPort ?? 0);

    Log.Information("Event Bus: {EventBus}", apiConfiguration.GetActiveEventBus());
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



