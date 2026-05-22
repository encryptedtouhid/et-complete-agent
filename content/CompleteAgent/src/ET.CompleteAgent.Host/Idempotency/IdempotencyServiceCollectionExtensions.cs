using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ET.CompleteAgent.Host.Idempotency;

internal static class IdempotencyServiceCollectionExtensions
{
    public static IServiceCollection AddIdempotency(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<IdempotencyMiddleware>();
        return services;
    }

    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}
