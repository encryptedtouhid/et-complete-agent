using System.Threading.RateLimiting;
using EncryptedTouhid.CompleteAgent.Host.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EncryptedTouhid.CompleteAgent.Host.RateLimiting;

internal static class RateLimitServiceCollectionExtensions
{
    public static IServiceCollection AddAgentRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var opts = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new();

        if (opts.Store == RateLimitStoreKind.Redis)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(opts.RedisConnectionString));
            services.AddTransient<RedisRateLimitMiddleware>();
            return services;
        }

        services.AddRateLimiter(rateOpts =>
        {
            rateOpts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateOpts.AddPolicy("agent", context =>
            {
                var limits = context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                var partitionKey = context.Request.Headers[ApiKeyOptions.HeaderName].ToString();
                if (string.IsNullOrEmpty(partitionKey))
                {
                    partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                }
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseAgentRateLimiting(this IApplicationBuilder app, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configuration);

        var opts = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new();
        if (opts.Store == RateLimitStoreKind.Redis)
        {
            return app.UseMiddleware<RedisRateLimitMiddleware>();
        }
        return app.UseRateLimiter();
    }
}
