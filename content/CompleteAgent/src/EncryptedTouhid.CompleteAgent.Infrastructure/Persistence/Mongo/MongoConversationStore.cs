using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Mongo;

public sealed class MongoConversationStore : IConversationStore
{
    private readonly IMongoCollection<MongoConversationMessage> _collection;
    private readonly TimeProvider _timeProvider;
    private readonly ConversationOptions _options;

    public MongoConversationStore(
        IMongoDatabase database,
        IOptions<PersistenceOptions> persistence,
        IOptions<ConversationOptions> conversation,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(persistence);
        ArgumentNullException.ThrowIfNull(conversation);

        _collection = database.GetCollection<MongoConversationMessage>(
            persistence.Value.Mongo.ConversationsCollection);
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _options = conversation.Value;
    }

    public async Task<IReadOnlyList<ChatMessage>> LoadAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        var filter = Builders<MongoConversationMessage>.Filter.Eq(x => x.ConversationId, conversationId);
        var sort = Builders<MongoConversationMessage>.Sort.Ascending(x => x.CreatedAt);

        var rows = await _collection
            .Find(filter)
            .Sort(sort)
            .ToListAsync(cancellationToken);

        return rows
            .Select(m => new ChatMessage(ConversationRoleMap.From(m.Role), m.Content))
            .ToArray();
    }

    public async Task AppendAsync(string conversationId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(message);

        await _collection.InsertOneAsync(
            new MongoConversationMessage
            {
                ConversationId = conversationId,
                Role = ConversationRoleMap.To(message.Role),
                Content = message.Text ?? string.Empty,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            },
            options: null,
            cancellationToken: cancellationToken);

        await TrimOverflowAsync(conversationId, cancellationToken);
    }

    public async Task ClearAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        var filter = Builders<MongoConversationMessage>.Filter.Eq(x => x.ConversationId, conversationId);
        await _collection.DeleteManyAsync(filter, cancellationToken);
    }

    private async Task TrimOverflowAsync(string conversationId, CancellationToken cancellationToken)
    {
        var filter = Builders<MongoConversationMessage>.Filter.Eq(x => x.ConversationId, conversationId);
        var sort = Builders<MongoConversationMessage>.Sort.Descending(x => x.CreatedAt);

        var overflowIds = await _collection
            .Find(filter)
            .Sort(sort)
            .Skip(_options.MaxMessagesPerConversation)
            .Project(x => x.Id)
            .ToListAsync(cancellationToken);

        if (overflowIds.Count == 0)
        {
            return;
        }

        var deleteFilter = Builders<MongoConversationMessage>.Filter.In(x => x.Id, overflowIds);
        await _collection.DeleteManyAsync(deleteFilter, cancellationToken);
    }
}
