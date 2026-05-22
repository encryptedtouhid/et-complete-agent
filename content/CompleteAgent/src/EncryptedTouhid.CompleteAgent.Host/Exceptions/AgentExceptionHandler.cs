using System.ClientModel;
using System.Text.Json;
using Azure;
using EncryptedTouhid.CompleteAgent.Host.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EncryptedTouhid.CompleteAgent.Host.Exceptions;

internal sealed partial class AgentExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AgentExceptionHandler> _logger;

    public AgentExceptionHandler(ILogger<AgentExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (status, message) = exception switch
        {
            RequestFailedException ex => (StatusCodes.Status502BadGateway, $"Upstream provider returned {ex.Status}"),
            ClientResultException ex => (StatusCodes.Status502BadGateway, $"Upstream provider error: {ex.Status}"),
            HttpRequestException => (StatusCodes.Status502BadGateway, "Upstream provider unreachable"),
            TimeoutException => (StatusCodes.Status504GatewayTimeout, "Upstream provider timed out"),
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Request cancelled"),
            _ => (0, string.Empty)
        };

        if (status == 0)
        {
            return false;
        }

        LogHandled(exception.GetType().Name, status);

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        var payload = JsonSerializer.Serialize(
            new AgentErrorResponse(message),
            AgentJsonContext.Default.AgentErrorResponse);
        await httpContext.Response.WriteAsync(payload, cancellationToken);
        return true;
    }

    [LoggerMessage(LogLevel.Warning, "Handled {ExceptionType} as HTTP {Status}")]
    private partial void LogHandled(string exceptionType, int status);
}
