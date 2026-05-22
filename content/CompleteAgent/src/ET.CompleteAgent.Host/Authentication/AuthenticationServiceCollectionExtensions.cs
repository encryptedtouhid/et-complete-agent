using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ET.CompleteAgent.Host.Authentication;

internal static class AuthenticationServiceCollectionExtensions
{
    public const string PolicyAgent = "AgentAccess";

    public static IServiceCollection AddAgentAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddApiKeyAuthentication(configuration);

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName));

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();

        if (jwt.Enabled)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = jwt.Authority;
                    options.Audience = jwt.Audience;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = jwt.ValidIssuers.Count > 0,
                        ValidIssuers = jwt.ValidIssuers,
                        ValidateAudience = !string.IsNullOrWhiteSpace(jwt.Audience),
                        ValidAudience = jwt.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds)
                    };
                });

            services
                .AddAuthorizationBuilder()
                .AddPolicy(PolicyAgent, policy => policy.RequireAuthenticatedUser());
        }
        else
        {
            services
                .AddAuthorizationBuilder()
                .AddPolicy(PolicyAgent, policy => policy.RequireAssertion(_ => true));
        }

        return services;
    }
}
