using System.ComponentModel.DataAnnotations;

namespace ET.CompleteAgent.Application.Conversations;

public sealed class ConversationOptions
{
    public const string SectionName = "Conversation";

    [Range(1, 1440)]
    public int TtlMinutes { get; init; } = 60;

    [Range(1, 1000)]
    public int MaxMessagesPerConversation { get; init; } = 50;
}
