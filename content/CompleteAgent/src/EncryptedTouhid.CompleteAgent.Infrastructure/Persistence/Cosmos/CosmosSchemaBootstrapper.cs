using System.Diagnostics.CodeAnalysis;
using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Idempotently creates the Cosmos database + containers on startup. Sets container-level
/// TTL (in seconds) to the configured Conversation TTL so expired turns auto-evict.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by IServiceCollection.AddHostedService<T>().")]
internal sealed partial class CosmosSchemaBootstrapper : IHostedService
{
    private readonly CosmosClient _client;
    private readonly CosmosOptions _cosmos;
    private readonly ConversationOptions _conversation;
    private readonly ILogger<CosmosSchemaBootstrapper> _logger;

    public CosmosSchemaBootstrapper(
        CosmosClient client,
        IOptions<PersistenceOptions> persistence,
        IOptions<ConversationOptions> conversation,
        ILogger<CosmosSchemaBootstrapper> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentNullException.ThrowIfNull(persistence);
        ArgumentNullException.ThrowIfNull(conversation);
        _cosmos = persistence.Value.Cosmos;
        _conversation = conversation.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var dbResponse = await _client.CreateDatabaseIfNotExistsAsync(
            _cosmos.Database,
            throughput: _cosmos.DatabaseThroughput,
            cancellationToken: cancellationToken);
        var database = dbResponse.Database;

        var conversationsTtlSeconds = checked(_conversation.TtlMinutes * 60);
        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_cosmos.ConversationsContainer, "/conversationId")
            {
                DefaultTimeToLive = conversationsTtlSeconds
            },
            cancellationToken: cancellationToken);

        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_cosmos.AuditContainer, "/subjectId"),
            cancellationToken: cancellationToken);

        LogBootstrapped(_cosmos.Database, _cosmos.ConversationsContainer, _cosmos.AuditContainer);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(LogLevel.Information, "Cosmos schema bootstrap — db: {Database}, conversations: {Conversations}, audit: {Audit}")]
    private partial void LogBootstrapped(string database, string conversations, string audit);
}
