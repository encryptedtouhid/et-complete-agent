using Microsoft.Extensions.AI;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;

internal static class ConversationRoleMap
{
    public static ChatRole From(string role) => role switch
    {
        "user" => ChatRole.User,
        "assistant" => ChatRole.Assistant,
        "system" => ChatRole.System,
        "tool" => ChatRole.Tool,
        _ => new ChatRole(role)
    };

    public static string To(ChatRole role) => role.Value;
}
