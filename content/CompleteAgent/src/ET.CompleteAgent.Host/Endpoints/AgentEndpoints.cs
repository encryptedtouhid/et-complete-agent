using System.Text.Json;
using ET.CompleteAgent.Application.Conversations;
using ET.CompleteAgent.Application.Workflows;
using ET.CompleteAgent.Domain.Agents;
using ET.CompleteAgent.Host.Authentication;
using ET.CompleteAgent.Host.Logging;
using ET.CompleteAgent.Host.Models;
using ET.CompleteAgent.Infrastructure.Logging;

namespace ET.CompleteAgent.Host.Endpoints;

internal static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/agent")
            .RequireRateLimiting("agent")
            .RequireAuthorization(ET.CompleteAgent.Host.Authentication.AuthenticationServiceCollectionExtensions.PolicyAgent);

        group.MapPost("/run", RunAsync);
        group.MapPost("/stream", StreamAsync);
        group.MapPost("/classify", ClassifyAsync);
        group.MapPost("/workflow/research", ResearchWorkflowAsync);
        group.MapDelete("/conversations/{conversationId}", ClearConversationAsync);

        return app;
    }

    private static async Task<IResult> RunAsync(
        AgentInvokeRequest request,
        HttpContext context,
        IAgentRunner runner,
        ILogger<AgentInvokeRequest> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return Results.BadRequest(new AgentErrorResponse("Input is required."));
        }

        var subject = SubjectScoping.ResolveSubject(context);
        var scopedConversationId = SubjectScoping.ScopeConversationId(subject, request.ConversationId);

        AgentEndpointLog.RequestReceived(logger, PromptRedactor.Redact(request.Input));
        var result = await runner.RunAsync(
            AgentRequest.From(request.Input, scopedConversationId, subject),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new AgentInvokeResponse(result.Text ?? string.Empty))
            : Results.Problem(detail: result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    private static IResult StreamAsync(
        AgentInvokeRequest request,
        HttpContext context,
        IAgentRunner runner,
        ILogger<AgentInvokeRequest> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return Results.BadRequest(new AgentErrorResponse("Input is required."));
        }

        var subject = SubjectScoping.ResolveSubject(context);
        var scopedConversationId = SubjectScoping.ScopeConversationId(subject, request.ConversationId);
        var scopedRequest = AgentRequest.From(request.Input, scopedConversationId, subject);

        AgentEndpointLog.StreamRequestReceived(logger, PromptRedactor.Redact(request.Input));
        return Results.Stream(
            stream => WriteSseAsync(stream, runner, scopedRequest, cancellationToken),
            "text/event-stream");
    }

    private static async Task WriteSseAsync(
        Stream output,
        IAgentRunner runner,
        AgentRequest request,
        CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(output, leaveOpen: true) { AutoFlush = true };
        await foreach (var chunk in runner.StreamAsync(request, cancellationToken))
        {
            var data = JsonSerializer.Serialize(chunk, AgentJsonContext.Default.String);
            await writer.WriteAsync($"data: {data}\n\n").WaitAsync(cancellationToken);
        }
        await writer.WriteAsync("event: done\ndata: {}\n\n").WaitAsync(cancellationToken);
    }

    private static async Task<IResult> ClassifyAsync(
        AgentInvokeRequest request,
        HttpContext context,
        IAgentRunner runner,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return Results.BadRequest(new AgentErrorResponse("Input is required."));
        }

        var subject = SubjectScoping.ResolveSubject(context);

        const string schema = """{"sentiment": "positive" | "neutral" | "negative", "confidence": 0.0..1.0}""";
        var prompt = "Classify the sentiment of the user message. Respond with ONLY a JSON object matching this schema, no prose:\n"
                   + schema
                   + "\n\nMessage: " + request.Input;

        var result = await runner.RunAsync(AgentRequest.From(prompt, subjectId: subject), cancellationToken);
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Text))
        {
            return Results.Problem(detail: result.Error ?? "Empty response", statusCode: StatusCodes.Status500InternalServerError);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize(result.Text, AgentJsonContext.Default.SentimentClassification);
            return parsed is null
                ? Results.Problem(detail: "Agent response was not valid JSON.", statusCode: StatusCodes.Status502BadGateway)
                : Results.Ok(parsed);
        }
        catch (JsonException ex)
        {
            return Results.Problem(detail: $"Agent response was not valid JSON: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> ResearchWorkflowAsync(
        AgentInvokeRequest request,
        ResearchAndSummariseWorkflow workflow,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return Results.BadRequest(new AgentErrorResponse("Input is required."));
        }

        var result = await workflow.RunAsync(request.Input, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new AgentInvokeResponse(result.Text ?? string.Empty))
            : Results.Problem(detail: result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> ClearConversationAsync(
        string conversationId,
        HttpContext context,
        IConversationStore store,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return Results.BadRequest(new AgentErrorResponse("conversationId is required."));
        }

        var subject = SubjectScoping.ResolveSubject(context);
        var scoped = SubjectScoping.ScopeConversationId(subject, conversationId);
        await store.ClearAsync(scoped!, cancellationToken);
        return Results.NoContent();
    }
}
