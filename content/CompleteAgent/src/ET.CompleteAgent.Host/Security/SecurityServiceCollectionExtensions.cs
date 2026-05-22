using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ET.CompleteAgent.Host.Security;

internal static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddAgentSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<SecurityHeadersOptions>()
            .Bind(configuration.GetSection(SecurityHeadersOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateOnStart();

        services.AddTransient<SecurityHeadersMiddleware>();

        var corsOpts = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
        services.AddCors(o =>
        {
            o.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                if (!corsOpts.Enabled || corsOpts.AllowedOrigins.Count == 0)
                {
                    policy.DisallowCredentials();
                    return;
                }
                policy
                    .WithOrigins([.. corsOpts.AllowedOrigins])
                    .WithMethods("GET", "POST", "DELETE", "OPTIONS")
                    .WithHeaders("Content-Type", "Authorization", "X-API-Key", "Idempotency-Key")
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }

    public static IApplicationBuilder UseAgentSecurity(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
