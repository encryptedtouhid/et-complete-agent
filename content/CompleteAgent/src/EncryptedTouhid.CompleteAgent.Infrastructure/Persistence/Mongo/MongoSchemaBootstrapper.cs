using System.Diagnostics.CodeAnalysis;
using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Mongo;

/// <summary>
/// Idempotently ensures the conversation collection has:
///   - A compound index on (conversationId, createdAt) for fast load.
///   - A TTL index on createdAt that expires messages after ConversationOptions.TtlMinutes.
/// Mongo silently no-ops if the same index already exists with the same options.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by IServiceCollection.AddHostedService<T>().")]
internal sealed partial class MongoSchemaBootstrapper : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly MongoOptions _mongo;
    private readonly ConversationOptions _conversation;
    private readonly ILogger<MongoSchemaBootstrapper> _logger;

    public MongoSchemaBootstrapper(
        IMongoDatabase database,
        IOptions<PersistenceOptions> persistence,
        IOptions<ConversationOptions> conversation,
        ILogger<MongoSchemaBootstrapper> logger)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        ArgumentNullException.ThrowIfNull(persistence);
        ArgumentNullException.ThrowIfNull(conversation);
        _mongo = persistence.Value.Mongo;
        _conversation = conversation.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var conversations = _database.GetCollection<MongoConversationMessage>(_mongo.ConversationsCollection);

        var compoundKeys = Builders<MongoConversationMessage>.IndexKeys
            .Ascending(x => x.ConversationId)
            .Ascending(x => x.CreatedAt);
        await conversations.Indexes.CreateOneAsync(
            new CreateIndexModel<MongoConversationMessage>(compoundKeys, new CreateIndexOptions { Name = "ix_conversationId_createdAt" }),
            cancellationToken: cancellationToken);

        var ttlKeys = Builders<MongoConversationMessage>.IndexKeys.Ascending(x => x.CreatedAt);
        await conversations.Indexes.CreateOneAsync(
            new CreateIndexModel<MongoConversationMessage>(
                ttlKeys,
                new CreateIndexOptions
                {
                    Name = "ttl_createdAt",
                    ExpireAfter = TimeSpan.FromMinutes(_conversation.TtlMinutes)
                }),
            cancellationToken: cancellationToken);

        var audit = _database.GetCollection<MongoAuditEntry>(_mongo.AuditCollection);
        var auditKeys = Builders<MongoAuditEntry>.IndexKeys
            .Ascending(x => x.SubjectId)
            .Ascending(x => x.Timestamp);
        await audit.Indexes.CreateOneAsync(
            new CreateIndexModel<MongoAuditEntry>(auditKeys, new CreateIndexOptions { Name = "ix_subjectId_timestamp" }),
            cancellationToken: cancellationToken);

        LogBootstrapped(_database.DatabaseNamespace.DatabaseName, _mongo.ConversationsCollection, _mongo.AuditCollection);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(LogLevel.Information, "Mongo schema bootstrap — db: {Database}, conversations: {Conversations}, audit: {Audit}")]
    private partial void LogBootstrapped(string database, string conversations, string audit);
}
