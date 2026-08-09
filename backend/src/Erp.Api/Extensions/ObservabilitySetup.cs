using System.Globalization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace Erp.Api.Extensions;

/// <summary>
/// Structured logs, traces and metrics.
/// <para>
/// Logs go to a log system — console in development, Seq or an OTLP collector
/// elsewhere. They never go to the application database, which is where the legacy
/// system wrote them: log queries competed with order entry for the same IO, and
/// there was no correlation id, so a user's reported failure could not be tied to
/// anything.
/// </para>
/// </summary>
internal static class ObservabilitySetup
{
    public const string ServiceName = "erp-api";

    public static void ConfigureErpSerilog(this IHostBuilder host) =>
        host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", ServiceName)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            // Invariant culture on both sinks: log output is machine-readable data,
            // and a number formatted with the server's locale is not greppable.
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Seq(
                context.Configuration["Observability:SeqUrl"] ?? "http://localhost:5341",
                formatProvider: CultureInfo.InvariantCulture));

    public static IServiceCollection AddErpObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["Observability:OtlpEndpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                        // Health probes would otherwise dominate the trace volume.
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation()
                    .AddSource("Microsoft.EntityFrameworkCore");

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }
}
