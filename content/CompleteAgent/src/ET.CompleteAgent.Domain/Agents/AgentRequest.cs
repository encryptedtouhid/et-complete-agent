namespace ET.CompleteAgent.Domain.Agents;

public sealed record AgentRequest(string UserInput, string? ConversationId = null)
{
    public static AgentRequest From(string userInput, string? conversationId = null) =>
        new(userInput ?? throw new ArgumentNullException(nameof(userInput)), conversationId);
}
