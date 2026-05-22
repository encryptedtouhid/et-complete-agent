namespace ET.CompleteAgent.Domain.Agents;

public sealed record AgentResult(
    bool IsSuccess,
    string? Text,
    string? Error)
{
    public static AgentResult Success(string text) => new(true, text, null);

    public static AgentResult Failure(string error) => new(false, null, error);
}
