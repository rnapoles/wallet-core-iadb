using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WalletSystem.Shared.Settings.Extensions;
using WalletSystem.Worker.EventListeners.Settings.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Configuration priority (highest to lowest):
// 1. Environment Variables
// 2. .env file
// 3. appsettings.json
builder.Configuration.AddEnvironmentFile();
builder.Configuration.AddEnvironmentVariables();

// Services
builder.Services.AddMessagingServices(builder.Configuration);

// Register your background worker service
//builder.Services.AddHostedService<WalletSystem.Worker>();

// Build and run the daemon application
IHost host = builder.Build();
host.Run();