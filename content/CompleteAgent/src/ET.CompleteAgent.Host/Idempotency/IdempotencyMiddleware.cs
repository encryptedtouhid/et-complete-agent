using System.Security.Cryptography;
using System.Text;
using ET.CompleteAgent.Host.Endpoints;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ET.CompleteAgent.Host.Idempotency;

internal sealed partial class IdempotencyMiddleware : IMiddleware
{
    private static readonly string[] AppliesToPaths =
    [
        "/agent/run",
        "/agent/classify",
        "/agent/workflow/research"
    ];

    private readonly IMemoryCache _cache;
    private readonly IdempotencyOptions _options;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        IMemoryCache cache,
        IOptions<IdempotencyOptions> options,
        ILogger<IdempotencyMiddleware> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!_options.Enabled || !ShouldIntercept(context))
        {
            await next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[IdempotencyOptions.HeaderName].ToString();
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            await next(context);
            return;
        }
        if (idempotencyKey.Length > _options.MaxKeyLength)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(
                $"Idempotency-Key exceeds maximum length of {_options.MaxKeyLength}.",
                context.RequestAborted);
            return;
        }

        var cacheKey = await BuildCacheKeyAsync(context, idempotencyKey);

        if (_cache.TryGetValue<CachedResponse>(cacheKey, out var cached) && cached is not null)
        {
            LogReplay(idempotencyKey, cached.StatusCode);
            await ReplayAsync(context, cached);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);
            buffer.Position = 0;
            var bodyBytes = buffer.ToArray();

            await originalBody.WriteAsync(bodyBytes, context.RequestAborted);

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                _cache.Set(
                    cacheKey,
                    new CachedResponse(context.Response.StatusCode, context.Response.ContentType, bodyBytes),
                    TimeSpan.FromMinutes(_options.TtlMinutes));
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldIntercept(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        return HttpMethods.IsPost(context.Request.Method)
            && AppliesToPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string> BuildCacheKeyAsync(HttpContext context, string idempotencyKey)
    {
        var subject = SubjectScoping.ResolveSubject(context);

        context.Request.EnableBuffering();
        var bodyHash = string.Empty;
        if (context.Request.ContentLength > 0)
        {
            using var hasher = SHA256.Create();
            context.Request.Body.Position = 0;
            var hash = await hasher.ComputeHashAsync(context.Request.Body, context.RequestAborted);
            bodyHash = Convert.ToHexStringLower(hash);
            context.Request.Body.Position = 0;
        }

        var raw = $"{subject}:{context.Request.Path}:{idempotencyKey}:{bodyHash}";
        return "idem:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    private static async Task ReplayAsync(HttpContext context, CachedResponse cached)
    {
        context.Response.StatusCode = cached.StatusCode;
        if (!string.IsNullOrEmpty(cached.ContentType))
        {
            context.Response.ContentType = cached.ContentType;
        }
        context.Response.Headers["Idempotency-Replay"] = "true";
        await context.Response.Body.WriteAsync(cached.Body, context.RequestAborted);
    }

    [LoggerMessage(LogLevel.Information, "Idempotency replay for key={Key} status={Status}")]
    private partial void LogReplay(string key, int status);

    private sealed record CachedResponse(int StatusCode, string? ContentType, byte[] Body);
}
