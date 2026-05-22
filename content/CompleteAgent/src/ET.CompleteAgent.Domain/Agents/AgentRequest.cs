namespace ET.CompleteAgent.Domain.Agents;

public sealed record AgentRequest(
    string UserInput,
    string? ConversationId = null,
    string? SubjectId = null)
{
    public static AgentRequest From(string userInput, string? conversationId = null, string? subjectId = null) =>
        new(
            userInput ?? throw new ArgumentNullException(nameof(userInput)),
            conversationId,
            subjectId);
}
