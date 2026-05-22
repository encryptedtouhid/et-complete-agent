using ET.CompleteAgent.Application.Agents;
using ET.CompleteAgent.Application.Audit;
using ET.CompleteAgent.Application.Budgeting;
using ET.CompleteAgent.Application.Conversations;
using ET.CompleteAgent.Application.Moderation;
using ET.CompleteAgent.Application.Prompts;
using ET.CompleteAgent.Application.Resilience;
using ET.CompleteAgent.Application.Tools;
using ET.CompleteAgent.Application.Workflows;
using ET.CompleteAgent.Domain.Agents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ET.CompleteAgent.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration, string promptsRoot)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptsRoot);

        services
            .AddOptions<ResilienceOptions>()
            .Bind(configuration.GetSection(ResilienceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<ConversationOptions>()
            .Bind(configuration.GetSection(ConversationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPromptLoader>(_ => new FileSystemPromptLoader(promptsRoot));
        services.AddSingleton<GetCurrentTimeTool>();
        services.AddSingleton<SearchKnowledgeBaseTool>();

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ResilienceOptions>>().Value;
            return new RetryPolicy(
                opts.MaxRetryAttempts,
                TimeSpan.FromSeconds(opts.BackoffSeconds),
                sp.GetRequiredService<ILogger<RetryPolicy>>());
        });

        services.TryAddContentModerator();
        services.TryAddTokenUsageTracker();
        services.TryAddAuditLog();
        services.AddSingleton<IAgentRunner, AgentRunner>();
        services.AddSingleton<ResearchAndSummariseWorkflow>();
        return services;
    }

    private static void TryAddContentModerator(this IServiceCollection services)
    {
        var hasModerator = services.Any(d => d.ServiceType == typeof(IContentModerator));
        if (!hasModerator)
        {
            services.AddSingleton<IContentModerator, NoOpContentModerator>();
        }
    }

    private static void TryAddTokenUsageTracker(this IServiceCollection services)
    {
        var hasTracker = services.Any(d => d.ServiceType == typeof(ITokenUsageTracker));
        if (!hasTracker)
        {
            services.AddSingleton<ITokenUsageTracker, InMemoryTokenUsageTracker>();
        }
    }

    private static void TryAddAuditLog(this IServiceCollection services)
    {
        var hasAudit = services.Any(d => d.ServiceType == typeof(IAuditLog));
        if (!hasAudit)
        {
            services.AddSingleton<IAuditLog, NoOpAuditLog>();
        }
    }
}
