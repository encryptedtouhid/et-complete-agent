using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EncryptedTouhid.CompleteAgent.Host.HealthChecks;

internal sealed class CosmosHealthCheck : IHealthCheck
{
    private readonly CosmosClient _client;
    private readonly CosmosOptions _cosmos;

    public CosmosHealthCheck(CosmosClient client, IOptions<PersistenceOptions> persistence)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentNullException.ThrowIfNull(persistence);
        _cosmos = persistence.Value.Cosmos;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _client.GetDatabase(_cosmos.Database);
            await db.ReadAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy($"Cosmos database '{_cosmos.Database}' reachable");
        }
        catch (CosmosException ex)
        {
            return HealthCheckResult.Unhealthy("Cosmos DB error", ex);
        }
    }
}
