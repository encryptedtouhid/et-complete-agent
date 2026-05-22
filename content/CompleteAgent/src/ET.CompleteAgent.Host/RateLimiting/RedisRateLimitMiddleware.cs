using System.Globalization;
using ET.CompleteAgent.Host.Authentication;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ET.CompleteAgent.Host.RateLimiting;

internal sealed class RedisRateLimitMiddleware : IMiddleware
{
    private const string LuaScript = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        return current
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly RateLimitOptions _options;

    public RedisRateLimitMiddleware(IConnectionMultiplexer redis, IOptions<RateLimitOptions> options)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!context.Request.Path.StartsWithSegments("/agent", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var partition = context.Request.Headers[ApiKeyOptions.HeaderName].ToString();
        if (string.IsNullOrEmpty(partition))
        {
            partition = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        var window = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / _options.WindowSeconds;
        var key = (RedisKey)$"rl:{partition}:{window.ToString(CultureInfo.InvariantCulture)}";
        var db = _redis.GetDatabase();

        var result = await db.ScriptEvaluateAsync(
            LuaScript,
            [key],
            [_options.WindowSeconds]);

        var count = (long)result;
        if (count > _options.PermitLimit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = _options.WindowSeconds.ToString(CultureInfo.InvariantCulture);
            return;
        }

        await next(context);
    }
}
