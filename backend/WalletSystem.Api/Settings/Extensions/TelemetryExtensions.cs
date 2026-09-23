using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WalletSystem.Api.Settings.Extensions;

public static class TelemetryExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("WalletSystem.Api.MediatR"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddSqlClientInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("https://log-server:5341/ingest/otlp/v1/traces");
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                })
            )
            .WithMetrics(metrics => metrics
                .AddMeter("AppExporter")
                .AddMeter("System.Runtime")
                .AddMeter("System.Net.NameResolution")
                .AddMeter("System.Net.Http")
                .AddMeter("Microsoft.AspNetCore.Components")
                .AddMeter("Microsoft.AspNetCore.Components.Lifecycle")
                .AddMeter("Microsoft.AspNetCore.Components.Server.Circuits")
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Routing")
                .AddMeter("Microsoft.AspNetCore.Diagnostics")
                .AddMeter("Microsoft.AspNetCore.RateLimiting")
                .AddMeter("Microsoft.AspNetCore.HeaderParsing")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddMeter("Microsoft.AspNetCore.Http.Connections")
                .AddMeter("Microsoft.AspNetCore.Authorization")
                .AddMeter("Microsoft.AspNetCore.Authentication")
                .AddMeter("Microsoft.Extensions.Diagnostics.HealthChecks")
                .AddMeter("Microsoft.Extensions.Diagnostics.ResourceMonitoring")
                .AddMeter("Microsoft.EntityFrameworkCore")
                .AddOtlpExporter((opt, metricReaderOptions) =>
                {
                    opt.Endpoint = new Uri("https://log-server:5341/ingest/otlp/v1/metrics");
                    opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                    //opt.Headers = "X-Seq-ApiKey=abcde12345";
                    //metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10_000;
                    //metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                })
            )
            .WithLogging(logging => {
                logging.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("https://log-server:5341/ingest/otlp/v1/logs");
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                });
            })
            ;    
        
        return services;
    }
}