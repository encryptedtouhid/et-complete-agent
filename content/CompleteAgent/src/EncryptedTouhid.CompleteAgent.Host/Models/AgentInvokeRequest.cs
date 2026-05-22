namespace EncryptedTouhid.CompleteAgent.Host.Models;

internal sealed record AgentInvokeRequest(string Input, string? ConversationId = null);
