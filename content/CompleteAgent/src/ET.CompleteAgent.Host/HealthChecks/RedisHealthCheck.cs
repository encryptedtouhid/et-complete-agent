using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ET.CompleteAgent.Host.HealthChecks;

internal sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await _redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis ping {latency.TotalMilliseconds:F0}ms");
        }
        catch (RedisConnectionException ex)
        {
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
        catch (RedisTimeoutException ex)
        {
            return HealthCheckResult.Unhealthy("Redis timed out", ex);
        }
    }
}
