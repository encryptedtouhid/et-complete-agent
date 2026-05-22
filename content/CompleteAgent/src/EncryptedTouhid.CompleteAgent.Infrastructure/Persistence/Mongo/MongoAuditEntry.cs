using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Mongo;

internal sealed class MongoAuditEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("subjectId")]
    public string SubjectId { get; set; } = string.Empty;

    [BsonElement("conversationId")]
    public string? ConversationId { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("operation")]
    public string Operation { get; set; } = string.Empty;

    [BsonElement("inputPreview")]
    public string InputPreview { get; set; } = string.Empty;

    [BsonElement("success")]
    public bool Success { get; set; }

    [BsonElement("tokenCount")]
    public long TokenCount { get; set; }

    [BsonElement("durationMs")]
    public long DurationMs { get; set; }
}
