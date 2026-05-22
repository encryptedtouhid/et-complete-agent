using EncryptedTouhid.CompleteAgent.Application.Agents;
using EncryptedTouhid.CompleteAgent.Application.Prompts;
using EncryptedTouhid.CompleteAgent.Domain.Agents;

namespace EncryptedTouhid.CompleteAgent.Application.Workflows;

public sealed class ResearchAndSummariseWorkflow
{
    private readonly IChatAgentFactory _agentFactory;

    public ResearchAndSummariseWorkflow(IChatAgentFactory agentFactory)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
    }

    public async Task<AgentResult> RunAsync(string topic, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var researcher = _agentFactory.Create(
            name: "Researcher",
            instructions: "You list 3-5 concise, factual bullet points about the given topic. No opinions.");

        var summariser = _agentFactory.Create(
            name: "Summariser",
            instructions: "You receive bullet points and produce a single 2-sentence executive summary. Plain language. No marketing tone.");

        var wrappedTopic = InputSanitiser.Wrap(topic);

        var research = await researcher.RunAsync(wrappedTopic, cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(research.Text))
        {
            return AgentResult.Failure("Research step returned no content.");
        }

        var summary = await summariser.RunAsync(research.Text, cancellationToken: cancellationToken);
        return string.IsNullOrWhiteSpace(summary.Text)
            ? AgentResult.Failure("Summary step returned no content.")
            : AgentResult.Success(summary.Text);
    }
}
