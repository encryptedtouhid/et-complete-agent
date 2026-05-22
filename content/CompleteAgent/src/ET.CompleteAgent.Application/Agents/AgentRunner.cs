using System.Diagnostics;
using System.Runtime.CompilerServices;
using ET.CompleteAgent.Application.Audit;
using ET.CompleteAgent.Application.Budgeting;
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
    private const int AuditPreviewLength = 200;

    private readonly IChatAgentFactory _agentFactory;
    private readonly IPromptLoader _promptLoader;
    private readonly IConversationStore _conversationStore;
    private readonly IContentModerator _moderator;
    private readonly ITokenUsageTracker _usageTracker;
    private readonly IAuditLog _auditLog;
    private readonly TimeProvider _timeProvider;
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
        ITokenUsageTracker usageTracker,
        IAuditLog auditLog,
        TimeProvider timeProvider,
        GetCurrentTimeTool timeTool,
        SearchKnowledgeBaseTool searchTool,
        RetryPolicy retryPolicy,
        ILogger<AgentRunner> logger)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _promptLoader = promptLoader ?? throw new ArgumentNullException(nameof(promptLoader));
        _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
        _moderator = moderator ?? throw new ArgumentNullException(nameof(moderator));
        _usageTracker = usageTracker ?? throw new ArgumentNullException(nameof(usageTracker));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _timeTool = timeTool ?? throw new ArgumentNullException(nameof(timeTool));
        _searchTool = searchTool ?? throw new ArgumentNullException(nameof(searchTool));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> RunAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var activity = AgentDiagnostics.ActivitySource.StartActivity("agent.run", ActivityKind.Internal);
        activity?.SetTag("agent.subject_id", request.SubjectId);
        activity?.SetTag("agent.conversation_id", request.ConversationId);

        var sw = Stopwatch.StartNew();
        long capturedTokens = 0;
        var success = false;
        AgentResult result;

        var inputModeration = await _moderator.ModerateAsync(request.UserInput, cancellationToken);
        if (!inputModeration.IsAllowed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Input blocked by moderation");
            result = AgentResult.Failure("Input was rejected by content moderation.");
            await TryAuditAsync(request, "run", success: false, 0, sw.ElapsedMilliseconds, cancellationToken);
            return result;
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
                capturedTokens = usage.TotalTokenCount ?? 0;
                LogUsage(usage.InputTokenCount ?? 0, usage.OutputTokenCount ?? 0, capturedTokens);
                activity?.SetTag("ai.tokens.input", usage.InputTokenCount);
                activity?.SetTag("ai.tokens.output", usage.OutputTokenCount);
                activity?.SetTag("ai.tokens.total", usage.TotalTokenCount);

                if (!string.IsNullOrWhiteSpace(request.SubjectId) && capturedTokens > 0)
                {
                    _usageTracker.Increment(
                        request.SubjectId,
                        DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime),
                        capturedTokens);
                }
            }

            var rawText = response.Text ?? string.Empty;

            var outputModeration = await _moderator.ModerateAsync(rawText, cancellationToken);
            if (!outputModeration.IsAllowed)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Output blocked by moderation");
                result = AgentResult.Failure("Output was rejected by content moderation.");
            }
            else
            {
                var scrubbed = OutputGuardrail.Scrub(rawText);
                await PersistTurnAsync(request, scrubbed, cancellationToken);
                activity?.SetStatus(ActivityStatusCode.Ok);
                success = true;
                result = AgentResult.Success(scrubbed);
            }
        }
        catch (HttpRequestException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Network error reaching the LLM provider");
            result = AgentResult.Failure("The agent could not reach the model provider.");
        }
        catch (TimeoutException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "LLM call timed out");
            result = AgentResult.Failure("The agent timed out.");
        }
        catch (InvalidOperationException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Agent configuration is invalid");
            result = AgentResult.Failure("The agent is not configured correctly.");
        }

        await TryAuditAsync(request, "run", success, capturedTokens, sw.ElapsedMilliseconds, cancellationToken);
        return result;
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
        var sw = Stopwatch.StartNew();
        var inputModeration = await _moderator.ModerateAsync(request.UserInput, cancellationToken);
        if (!inputModeration.IsAllowed)
        {
            await TryAuditAsync(request, "stream", success: false, 0, sw.ElapsedMilliseconds, cancellationToken);
            yield break;
        }

        var agent = await GetOrCreateAgentAsync(cancellationToken);
        var input = InputSanitiser.Wrap(request.UserInput);
        var history = await LoadHistoryAsync(request.ConversationId, cancellationToken);
        var conversation = BuildMessages(history, input);

        long? totalTokens = null;
        await foreach (var update in agent.RunStreamingAsync(conversation, cancellationToken: cancellationToken))
        {
            if (update.Contents?.OfType<UsageContent>().FirstOrDefault() is { } usageContent)
            {
                totalTokens = (totalTokens ?? 0) + (usageContent.Details.TotalTokenCount ?? 0);
            }
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return OutputGuardrail.Scrub(update.Text);
            }
        }

        var captured = totalTokens ?? 0;
        if (captured > 0 && !string.IsNullOrWhiteSpace(request.SubjectId))
        {
            _usageTracker.Increment(
                request.SubjectId,
                DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime),
                captured);
        }

        await TryAuditAsync(request, "stream", success: true, captured, sw.ElapsedMilliseconds, cancellationToken);
    }

    private async Task TryAuditAsync(
        AgentRequest request,
        string operation,
        bool success,
        long tokenCount,
        long durationMs,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = request.UserInput.Length > AuditPreviewLength
                ? request.UserInput[..AuditPreviewLength]
                : request.UserInput;

            await _auditLog.AppendAsync(
                new AuditEntry(
                    _timeProvider.GetUtcNow(),
                    request.SubjectId ?? "anonymous",
                    request.ConversationId,
                    operation,
                    preview,
                    success,
                    tokenCount,
                    durationMs),
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Audit append failed (network)");
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Audit append failed (timeout)");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Audit append failed (invalid op)");
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
