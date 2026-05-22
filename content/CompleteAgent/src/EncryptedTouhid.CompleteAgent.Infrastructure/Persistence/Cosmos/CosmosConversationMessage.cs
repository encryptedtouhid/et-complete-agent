using Newtonsoft.Json;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Cosmos;

internal sealed class CosmosConversationMessage
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonProperty("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    [JsonProperty("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Monotonic-per-conversation ordering token (UTC ticks). Used to sort messages
    /// reliably when multiple writers append within the same millisecond.
    /// </summary>
    [JsonProperty("sequence")]
    public long Sequence { get; set; }
}
