using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ET.CompleteAgent.Host.Authentication;

internal sealed class ApiKeyAuthenticationMiddleware : IMiddleware
{
    private static readonly string[] BypassPaths = ["/healthz", "/readyz", "/openapi", "/scalar"];

    private readonly byte[][] _expectedKeys;

    public ApiKeyAuthenticationMiddleware(IOptions<ApiKeyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _expectedKeys = [.. options.Value.ApiKeys.Select(k => Encoding.UTF8.GetBytes(k))];
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
        var matched = _expectedKeys.Any(expected =>
            CryptographicOperations.FixedTimeEquals(providedBytes, expected));

        return matched
            ? next(context)
            : Unauthorized(context, "API key invalid");
    }

    private static Task Unauthorized(HttpContext context, string reason)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = $"ApiKey realm=\"agent\", error=\"{reason}\"";
        return Task.CompletedTask;
    }
}
