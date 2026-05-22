using System.Text.Json;
using ET.CompleteAgent.Application.Conversations;
using ET.CompleteAgent.Domain.Agents;
using ET.CompleteAgent.Host.Logging;
using ET.CompleteAgent.Host.Models;
using ET.CompleteAgent.Infrastructure.Logging;

namespace ET.CompleteAgent.Host.Endpoints;

internal static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/agent/run", RunAsync).RequireRateLimiting("agent");
        app.MapPost("/agent/stream", StreamAsync).RequireRateLimiting("agent");
        app.MapPost("/agent/classify", ClassifyAsync).RequireRateLimiting("agent");
        app.MapDelete("/agent/conversations/{conversationId}", ClearConversationAsync).RequireRateLimiting("agent");

        return app;
    }

    private static async Task<IResult> RunAsync(
        AgentInvokeRequest request,
        IAgentRunner runner,
        ILogger<AgentInvokeRequest> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return Results.BadRequest(new AgentErrorResponse("Input is required."));
        }

        AgentEndpointLog.RequestReceived(logger, PromptRedactor.Redact(request.Input));
        var result = await runner.RunAsync(AgentRequest.From(request.Input, request.ConversationId), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new AgentInvokeResponse(result.Text ?? string.Empty))
            : Results.Problem(detail: result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    private static IResult StreamAsync(
        AgentInvokeRequest request,
        IAgentRunner runner,
        ILogger<AgentInvokeRequest> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return Results.BadRequest(new AgentErrorResponse("Input is required."));
        }

        AgentEndpointLog.StreamRequestReceived(logger, PromptRedactor.Redact(request.Input));
        return Results.Stream(
            stream => WriteSseAsync(stream, runner, request, cancellationToken),
            "text/event-stream");
    }

    private static async Task WriteSseAsync(
        Stream output,
        IAgentRunner runner,
        AgentInvokeRequest request,
        CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(output, leaveOpen: true) { AutoFlush = true };
        await foreach (var chunk in runner.StreamAsync(AgentRequest.From(request.Input, request.ConversationId), cancellationToken))
        {
            var data = JsonSerializer.Serialize(chunk, AgentJsonContext.Default.String);
            await writer.WriteAsync($"data: {data}\n\n").WaitAsync(cancellationToken);
        }
        await writer.WriteAsync("event: done\ndata: {}\n\n").WaitAsync(cancellationToken);
    }

    private static async Task<IResult> ClassifyAsync(
        AgentInvokeRequest request,
        IAgentRunner runner,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return Results.BadRequest(new AgentErrorResponse("Input is required."));
        }

        const string schema = """{"sentiment": "positive" | "neutral" | "negative", "confidence": 0.0..1.0}""";
        var prompt = "Classify the sentiment of the user message. Respond with ONLY a JSON object matching this schema, no prose:\n"
                   + schema
                   + "\n\nMessage: " + request.Input;

        var result = await runner.RunAsync(AgentRequest.From(prompt), cancellationToken);
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

    private static async Task<IResult> ClearConversationAsync(
        string conversationId,
        IConversationStore store,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return Results.BadRequest(new AgentErrorResponse("conversationId is required."));
        }
        await store.ClearAsync(conversationId, cancellationToken);
        return Results.NoContent();
    }
}
