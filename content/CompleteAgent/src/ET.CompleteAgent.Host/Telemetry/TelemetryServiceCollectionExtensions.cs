using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ET.CompleteAgent.Host.Telemetry;

internal static class TelemetryServiceCollectionExtensions
{
    public const string AgentActivitySource = "ET.CompleteAgent";

    public static IServiceCollection AddAgentTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<TelemetryOptions>()
            .Bind(configuration.GetSection(TelemetryOptions.SectionName));

        var opts = configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new TelemetryOptions();

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(opts.ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(AgentActivitySource)
                    .AddSource("Microsoft.Agents.AI")
                    .AddSource("Experimental.Microsoft.Extensions.AI")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (opts.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
                if (!string.IsNullOrWhiteSpace(opts.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(opts.OtlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter("Microsoft.Agents.AI")
                    .AddMeter("Microsoft.Extensions.AI")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (opts.EnableConsoleExporter)
                {
                    metrics.AddConsoleExporter();
                }
                if (!string.IsNullOrWhiteSpace(opts.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(opts.OtlpEndpoint));
                }
            });

        return services;
    }
}
