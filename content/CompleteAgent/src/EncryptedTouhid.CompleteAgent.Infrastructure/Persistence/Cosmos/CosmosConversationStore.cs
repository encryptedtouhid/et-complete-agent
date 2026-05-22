using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Cosmos;

public sealed class CosmosConversationStore : IConversationStore
{
    private readonly Container _container;
    private readonly TimeProvider _timeProvider;
    private readonly ConversationOptions _options;

    public CosmosConversationStore(
        CosmosClient client,
        IOptions<PersistenceOptions> persistence,
        IOptions<ConversationOptions> conversation,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(persistence);
        ArgumentNullException.ThrowIfNull(conversation);

        var cosmos = persistence.Value.Cosmos;
        _container = client.GetContainer(cosmos.Database, cosmos.ConversationsContainer);
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _options = conversation.Value;
    }

    public async Task<IReadOnlyList<ChatMessage>> LoadAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.conversationId = @id ORDER BY c.sequence ASC")
            .WithParameter("@id", conversationId);

        var partition = new PartitionKey(conversationId);
        var messages = new List<ChatMessage>();

        using var iterator = _container.GetItemQueryIterator<CosmosConversationMessage>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = partition });

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var msg in page)
            {
                messages.Add(new ChatMessage(ConversationRoleMap.From(msg.Role), msg.Content));
            }
        }

        return messages;
    }

    public async Task AppendAsync(string conversationId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(message);

        var now = _timeProvider.GetUtcNow();
        var item = new CosmosConversationMessage
        {
            ConversationId = conversationId,
            Role = ConversationRoleMap.To(message.Role),
            Content = message.Text ?? string.Empty,
            CreatedAt = now,
            Sequence = now.UtcTicks
        };

        await _container.CreateItemAsync(
            item,
            new PartitionKey(conversationId),
            cancellationToken: cancellationToken);

        await TrimOverflowAsync(conversationId, cancellationToken);
    }

    public async Task ClearAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        var partition = new PartitionKey(conversationId);
        var query = new QueryDefinition("SELECT c.id FROM c WHERE c.conversationId = @id")
            .WithParameter("@id", conversationId);

        using var iterator = _container.GetItemQueryIterator<IdOnly>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = partition });

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var row in page)
            {
                await _container.DeleteItemAsync<CosmosConversationMessage>(row.Id, partition, cancellationToken: cancellationToken);
            }
        }
    }

    private async Task TrimOverflowAsync(string conversationId, CancellationToken cancellationToken)
    {
        var partition = new PartitionKey(conversationId);
        var query = new QueryDefinition(
            "SELECT c.id FROM c WHERE c.conversationId = @id ORDER BY c.sequence DESC OFFSET @max LIMIT 1000")
            .WithParameter("@id", conversationId)
            .WithParameter("@max", _options.MaxMessagesPerConversation);

        using var iterator = _container.GetItemQueryIterator<IdOnly>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = partition });

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var row in page)
            {
                await _container.DeleteItemAsync<CosmosConversationMessage>(row.Id, partition, cancellationToken: cancellationToken);
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Cosmos SDK via reflection during deserialization.")]
    private sealed class IdOnly
    {
        [Newtonsoft.Json.JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
    }
}
