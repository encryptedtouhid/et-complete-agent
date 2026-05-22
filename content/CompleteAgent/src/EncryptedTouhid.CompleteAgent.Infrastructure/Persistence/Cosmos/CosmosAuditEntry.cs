using Newtonsoft.Json;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Cosmos;

internal sealed class CosmosAuditEntry
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonProperty("subjectId")]
    public string SubjectId { get; set; } = string.Empty;

    [JsonProperty("conversationId")]
    public string? ConversationId { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonProperty("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonProperty("inputPreview")]
    public string InputPreview { get; set; } = string.Empty;

    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("tokenCount")]
    public long TokenCount { get; set; }

    [JsonProperty("durationMs")]
    public long DurationMs { get; set; }
}
