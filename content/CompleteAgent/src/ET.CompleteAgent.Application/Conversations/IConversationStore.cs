using Microsoft.Extensions.AI;

namespace ET.CompleteAgent.Application.Conversations;

public interface IConversationStore
{
    Task<IReadOnlyList<ChatMessage>> LoadAsync(string conversationId, CancellationToken cancellationToken = default);

    Task AppendAsync(string conversationId, ChatMessage message, CancellationToken cancellationToken = default);

    Task ClearAsync(string conversationId, CancellationToken cancellationToken = default);
}
