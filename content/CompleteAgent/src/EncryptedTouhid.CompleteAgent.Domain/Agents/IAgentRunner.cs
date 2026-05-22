namespace EncryptedTouhid.CompleteAgent.Domain.Agents;

public interface IAgentRunner
{
    Task<AgentResult> RunAsync(AgentRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamAsync(AgentRequest request, CancellationToken cancellationToken = default);
}
