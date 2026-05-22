using EncryptedTouhid.CompleteAgent.Application.Agents;
using EncryptedTouhid.CompleteAgent.Application.Audit;
using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Application.Moderation;
using EncryptedTouhid.CompleteAgent.Application.Retrieval;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using EncryptedTouhid.CompleteAgent.Infrastructure.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Llm;
using EncryptedTouhid.CompleteAgent.Infrastructure.Moderation;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Audit;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Retrieval;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EncryptedTouhid.CompleteAgent.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<AgentOptions>()
            .Bind(configuration.GetSection(AgentOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(opts =>
            {
                var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(opts);
                return !opts.Validate(ctx).Any();
            }, "AgentOptions validation failed — see startup logs.")
            .ValidateOnStart();

        services
            .AddOptions<RetrievalOptions>()
            .Bind(configuration.GetSection(RetrievalOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<ModerationOptions>()
            .Bind(configuration.GetSection(ModerationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var moderation = configuration.GetSection(ModerationOptions.SectionName).Get<ModerationOptions>() ?? new();
        if (moderation.Provider == ModerationProviderKind.AzureContentSafety)
        {
            services.AddSingleton<IContentModerator, AzureContentSafetyModerator>();
        }

        services.AddSingleton<IChatAgentFactory, ChatAgentFactory>();
        services.AddMemoryCache();

        var persistence = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>() ?? new();
        AddPersistence(services, persistence);

        services.AddSingleton(sp =>
            EmbeddingGeneratorFactory.Create(
                sp.GetRequiredService<IOptions<AgentOptions>>(),
                sp.GetRequiredService<IOptions<RetrievalOptions>>()));

        AddVectorStore(services, configuration);

        return services;
    }

    private static void AddVectorStore(IServiceCollection services, IConfiguration configuration)
    {
        var retrieval = configuration.GetSection(RetrievalOptions.SectionName).Get<RetrievalOptions>() ?? new();

        if (retrieval.VectorStore == VectorStoreKind.Qdrant)
        {
            services.AddSingleton<IDocumentRetriever, QdrantDocumentRetriever>();
            return;
        }

        services.AddSingleton<IDocumentRetriever, InMemoryDocumentRetriever>();
    }

    private static void AddPersistence(IServiceCollection services, PersistenceOptions persistence)
    {
        if (persistence.ConversationStore == ConversationStoreKind.Sqlite)
        {
            services.AddDbContextFactory<AgentDbContext>(opts =>
                opts.UseSqlite(persistence.ConnectionString));
            services.AddSingleton<IConversationStore, EfCoreConversationStore>();
            services.AddSingleton<IAuditLog, EfCoreAuditLog>();
            return;
        }

        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        services.AddSingleton<IAuditLog, NoOpAuditLog>();
    }
}
