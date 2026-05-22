using EncryptedTouhid.CompleteAgent.Application.Audit;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Cosmos;

public sealed class CosmosAuditLog : IAuditLog
{
    private readonly Container _container;

    public CosmosAuditLog(CosmosClient client, IOptions<PersistenceOptions> persistence)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(persistence);

        var cosmos = persistence.Value.Cosmos;
        _container = client.GetContainer(cosmos.Database, cosmos.AuditContainer);
    }

    public async Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var item = new CosmosAuditEntry
        {
            SubjectId = entry.SubjectId,
            ConversationId = entry.ConversationId,
            Timestamp = entry.Timestamp,
            Operation = entry.Operation,
            InputPreview = entry.InputPreview,
            Success = entry.Success,
            TokenCount = entry.TokenCount,
            DurationMs = entry.DurationMs
        };

        await _container.CreateItemAsync(
            item,
            new PartitionKey(entry.SubjectId),
            cancellationToken: cancellationToken);
    }
}
