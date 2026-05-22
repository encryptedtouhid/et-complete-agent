using System.Diagnostics;
using System.Runtime.CompilerServices;
using ET.CompleteAgent.Application.Conversations;
using ET.CompleteAgent.Application.Guardrails;
using ET.CompleteAgent.Application.Moderation;
using ET.CompleteAgent.Application.Prompts;
using ET.CompleteAgent.Application.Resilience;
using ET.CompleteAgent.Application.Telemetry;
using ET.CompleteAgent.Application.Tools;
using ET.CompleteAgent.Domain.Agents;
using ET.CompleteAgent.Domain.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ET.CompleteAgent.Application.Agents;

public sealed partial class AgentRunner : IAgentRunner
{
    private readonly IChatAgentFactory _agentFactory;
    private readonly IPromptLoader _promptLoader;
    private readonly IConversationStore _conversationStore;
    private readonly IContentModerator _moderator;
    private readonly GetCurrentTimeTool _timeTool;
    private readonly SearchKnowledgeBaseTool _searchTool;
    private readonly RetryPolicy _retryPolicy;
    private readonly ILogger<AgentRunner> _logger;
    private AIAgent? _agent;

    public AgentRunner(
        IChatAgentFactory agentFactory,
        IPromptLoader promptLoader,
        IConversationStore conversationStore,
        IContentModerator moderator,
        GetCurrentTimeTool timeTool,
        SearchKnowledgeBaseTool searchTool,
        RetryPolicy retryPolicy,
        ILogger<AgentRunner> logger)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _promptLoader = promptLoader ?? throw new ArgumentNullException(nameof(promptLoader));
        _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
        _moderator = moderator ?? throw new ArgumentNullException(nameof(moderator));
        _timeTool = timeTool ?? throw new ArgumentNullException(nameof(timeTool));
        _searchTool = searchTool ?? throw new ArgumentNullException(nameof(searchTool));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> RunAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var activity = AgentDiagnostics.ActivitySource.StartActivity("agent.run", ActivityKind.Internal);
        activity?.SetTag("agent.conversation_id", request.ConversationId);

        var inputModeration = await _moderator.ModerateAsync(request.UserInput, cancellationToken);
        if (!inputModeration.IsAllowed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Input blocked by moderation");
            return AgentResult.Failure("Input was rejected by content moderation.");
        }

        try
        {
            var input = InputSanitiser.Wrap(request.UserInput);
            var history = await LoadHistoryAsync(request.ConversationId, cancellationToken);

            var response = await _retryPolicy.ExecuteAsync(async ct =>
            {
                var agent = await GetOrCreateAgentAsync(ct);
                var conversation = BuildMessages(history, input);
                return await agent.RunAsync(conversation, cancellationToken: ct);
            }, cancellationToken);

            if (response.Usage is { } usage)
            {
                LogUsage(usage.InputTokenCount ?? 0, usage.OutputTokenCount ?? 0, usage.TotalTokenCount ?? 0);
                activity?.SetTag("ai.tokens.input", usage.InputTokenCount);
                activity?.SetTag("ai.tokens.output", usage.OutputTokenCount);
                activity?.SetTag("ai.tokens.total", usage.TotalTokenCount);
            }

            var rawText = response.Text ?? string.Empty;

            var outputModeration = await _moderator.ModerateAsync(rawText, cancellationToken);
            if (!outputModeration.IsAllowed)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Output blocked by moderation");
                return AgentResult.Failure("Output was rejected by content moderation.");
            }

            var scrubbed = OutputGuardrail.Scrub(rawText);
            await PersistTurnAsync(request, scrubbed, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return AgentResult.Success(scrubbed);
        }
        catch (HttpRequestException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Network error reaching the LLM provider");
            return AgentResult.Failure("The agent could not reach the model provider.");
        }
        catch (TimeoutException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "LLM call timed out");
            return AgentResult.Failure("The agent timed out.");
        }
        catch (InvalidOperationException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Agent configuration is invalid");
            return AgentResult.Failure("The agent is not configured correctly.");
        }
    }

    public IAsyncEnumerable<string> StreamAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return StreamCoreAsync(request, cancellationToken);
    }

    private async IAsyncEnumerable<string> StreamCoreAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var inputModeration = await _moderator.ModerateAsync(request.UserInput, cancellationToken);
        if (!inputModeration.IsAllowed)
        {
            yield break;
        }

        var agent = await GetOrCreateAgentAsync(cancellationToken);
        var input = InputSanitiser.Wrap(request.UserInput);
        var history = await LoadHistoryAsync(request.ConversationId, cancellationToken);
        var conversation = BuildMessages(history, input);

        var stream = agent.RunStreamingAsync(conversation, cancellationToken: cancellationToken)
            .Where(u => !string.IsNullOrEmpty(u.Text))
            .Select(u => OutputGuardrail.Scrub(u.Text!));

        await foreach (var text in stream.WithCancellation(cancellationToken))
        {
            yield return text;
        }
    }

    private async Task<IReadOnlyList<ChatMessage>> LoadHistoryAsync(string? conversationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return Array.Empty<ChatMessage>();
        }
        return await _conversationStore.LoadAsync(conversationId, cancellationToken);
    }

    private static List<ChatMessage> BuildMessages(IReadOnlyList<ChatMessage> history, string userInput)
    {
        var messages = new List<ChatMessage>(history.Count + 1);
        messages.AddRange(history);
        messages.Add(new ChatMessage(ChatRole.User, userInput));
        return messages;
    }

    private async Task PersistTurnAsync(AgentRequest request, string assistantText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            return;
        }
        await _conversationStore.AppendAsync(request.ConversationId, new ChatMessage(ChatRole.User, request.UserInput), cancellationToken);
        await _conversationStore.AppendAsync(request.ConversationId, new ChatMessage(ChatRole.Assistant, assistantText), cancellationToken);
    }

    private async Task<AIAgent> GetOrCreateAgentAsync(CancellationToken cancellationToken)
    {
        if (_agent is not null)
        {
            return _agent;
        }

        var instructions = await _promptLoader.LoadSystemPromptAsync(PromptVersion.V1, cancellationToken);

        AIFunction[] tools =
        [
            AIFunctionFactory.Create(_timeTool.GetCurrentTimeUtc),
            AIFunctionFactory.Create(_searchTool.Search)
        ];

        _agent = _agentFactory.Create("CompleteAgent", instructions, tools);
        return _agent;
    }

    [LoggerMessage(LogLevel.Information, "LLM usage — input: {Input} tokens, output: {Output} tokens, total: {Total} tokens")]
    private partial void LogUsage(long input, long output, long total);
}
