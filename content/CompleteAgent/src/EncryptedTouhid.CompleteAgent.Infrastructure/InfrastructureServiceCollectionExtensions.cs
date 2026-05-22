using System.ComponentModel.DataAnnotations;
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
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Cosmos;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Mongo;
using EncryptedTouhid.CompleteAgent.Infrastructure.Retrieval;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

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
                var ctx = new ValidationContext(opts);
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
            .Validate(opts =>
            {
                var ctx = new ValidationContext(opts);
                return !opts.Validate(ctx).Any();
            }, "PersistenceOptions validation failed — see startup logs.")
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
        switch (persistence.ConversationStore)
        {
            case ConversationStoreKind.Sqlite:
            case ConversationStoreKind.SqlServer:
            case ConversationStoreKind.AzureSql:
            case ConversationStoreKind.Postgres:
            case ConversationStoreKind.MySql:
                AddRelational(services, persistence);
                return;

            case ConversationStoreKind.Cosmos:
                AddCosmos(services, persistence);
                return;

            case ConversationStoreKind.Mongo:
                AddMongo(services, persistence);
                return;

            default:
                services.AddSingleton<IConversationStore, InMemoryConversationStore>();
                services.AddSingleton<IAuditLog, NoOpAuditLog>();
                return;
        }
    }

    private static void AddRelational(IServiceCollection services, PersistenceOptions persistence)
    {
        services.AddDbContextFactory<AgentDbContext>(opts =>
            ConfigureRelationalProvider(opts, persistence));

        services.AddSingleton<IConversationStore, EfCoreConversationStore>();
        services.AddSingleton<IAuditLog, EfCoreAuditLog>();
        services.AddHostedService<RelationalSchemaBootstrapper>();
    }

    private static void ConfigureRelationalProvider(DbContextOptionsBuilder opts, PersistenceOptions persistence)
    {
        var connStr = persistence.ConnectionString;

        switch (persistence.ConversationStore)
        {
            case ConversationStoreKind.Sqlite:
                opts.UseSqlite(connStr);
                break;
            case ConversationStoreKind.SqlServer:
            case ConversationStoreKind.AzureSql:
                opts.UseSqlServer(connStr, sql => sql.EnableRetryOnFailure());
                break;
            case ConversationStoreKind.Postgres:
                opts.UseNpgsql(connStr, npg => npg.EnableRetryOnFailure());
                break;
            case ConversationStoreKind.MySql:
                opts.UseMySQL(connStr);
                break;
            default:
                throw new InvalidOperationException(
                    $"ConfigureRelationalProvider called with non-relational kind: {persistence.ConversationStore}");
        }
    }

    private static void AddCosmos(IServiceCollection services, PersistenceOptions persistence)
    {
        services.AddSingleton(_ => CosmosClientFactory.Create(persistence.Cosmos));
        services.AddSingleton<IConversationStore, CosmosConversationStore>();
        services.AddSingleton<IAuditLog, CosmosAuditLog>();
        services.AddHostedService<CosmosSchemaBootstrapper>();
    }

    private static void AddMongo(IServiceCollection services, PersistenceOptions persistence)
    {
        services.AddSingleton<IMongoClient>(_ => new MongoClient(persistence.ConnectionString));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(persistence.Mongo.Database));
        services.AddSingleton<IConversationStore, MongoConversationStore>();
        services.AddSingleton<IAuditLog, MongoAuditLog>();
        services.AddHostedService<MongoSchemaBootstrapper>();
    }
}
