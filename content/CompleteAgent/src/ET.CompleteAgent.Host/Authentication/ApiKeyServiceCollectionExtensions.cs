using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ET.CompleteAgent.Host.Authentication;

internal static class ApiKeyServiceCollectionExtensions
{
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<ApiKeyOptions>()
            .Bind(configuration.GetSection(ApiKeyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<ApiKeyAuthenticationMiddleware>();

        return services;
    }

    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
