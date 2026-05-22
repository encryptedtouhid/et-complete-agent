using EncryptedTouhid.CompleteAgent.Application.Audit;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Mongo;

public sealed class MongoAuditLog : IAuditLog
{
    private readonly IMongoCollection<MongoAuditEntry> _collection;

    public MongoAuditLog(IMongoDatabase database, IOptions<PersistenceOptions> persistence)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(persistence);

        _collection = database.GetCollection<MongoAuditEntry>(persistence.Value.Mongo.AuditCollection);
    }

    public async Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await _collection.InsertOneAsync(
            new MongoAuditEntry
            {
                SubjectId = entry.SubjectId,
                ConversationId = entry.ConversationId,
                Timestamp = entry.Timestamp.UtcDateTime,
                Operation = entry.Operation,
                InputPreview = entry.InputPreview,
                Success = entry.Success,
                TokenCount = entry.TokenCount,
                DurationMs = entry.DurationMs
            },
            options: null,
            cancellationToken: cancellationToken);
    }
}
