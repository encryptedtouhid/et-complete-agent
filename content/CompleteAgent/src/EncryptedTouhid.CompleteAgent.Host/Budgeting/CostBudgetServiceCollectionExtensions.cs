using EncryptedTouhid.CompleteAgent.Application.Budgeting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EncryptedTouhid.CompleteAgent.Host.Budgeting;

internal static class CostBudgetServiceCollectionExtensions
{
    public static IServiceCollection AddCostBudgeting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CostBudgetOptions>()
            .Bind(configuration.GetSection(CostBudgetOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenUsageTracker, InMemoryTokenUsageTracker>();
        services.AddTransient<CostBudgetMiddleware>();

        return services;
    }

    public static IApplicationBuilder UseCostBudgeting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<CostBudgetMiddleware>();
    }
}
