using ET.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Qdrant.Client;

namespace ET.CompleteAgent.Host.HealthChecks;

internal sealed class QdrantHealthCheck : IHealthCheck, IDisposable
{
    private readonly QdrantClient _client;

    public QdrantHealthCheck(IOptions<RetrievalOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var q = options.Value.Qdrant;
        _client = new QdrantClient(q.Host, q.Port, q.UseTls, apiKey: q.ApiKey);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await _client.HealthAsync(cancellationToken);
            return HealthCheckResult.Healthy($"Qdrant version {info.Version}");
        }
        catch (Grpc.Core.RpcException ex)
        {
            return HealthCheckResult.Unhealthy("Qdrant unreachable", ex);
        }
    }

    public void Dispose() => _client.Dispose();
}
