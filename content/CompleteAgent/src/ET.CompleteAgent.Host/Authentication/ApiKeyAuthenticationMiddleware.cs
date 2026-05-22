using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ET.CompleteAgent.Host.Authentication;

internal sealed class ApiKeyAuthenticationMiddleware : IMiddleware
{
    private static readonly string[] BypassPaths = ["/healthz", "/readyz", "/openapi"];

    private readonly byte[] _expectedKeyBytes;

    public ApiKeyAuthenticationMiddleware(IOptions<ApiKeyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _expectedKeyBytes = Encoding.UTF8.GetBytes(options.Value.ApiKey);
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var path = context.Request.Path.Value ?? string.Empty;
        if (BypassPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            return next(context);
        }

        var providedKey = context.Request.Headers[ApiKeyOptions.HeaderName].ToString();
        if (string.IsNullOrEmpty(providedKey))
        {
            return Unauthorized(context, "API key missing");
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        if (!CryptographicOperations.FixedTimeEquals(providedBytes, _expectedKeyBytes))
        {
            return Unauthorized(context, "API key invalid");
        }

        return next(context);
    }

    private static Task Unauthorized(HttpContext context, string reason)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = $"ApiKey realm=\"agent\", error=\"{reason}\"";
        return Task.CompletedTask;
    }
}
