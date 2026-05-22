using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EncryptedTouhid.CompleteAgent.Host.HealthChecks;

internal sealed class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;

    public MongoHealthCheck(IMongoDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy($"Mongo database '{_database.DatabaseNamespace.DatabaseName}' reachable");
        }
        catch (MongoException ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB error", ex);
        }
        catch (TimeoutException ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB timeout", ex);
        }
    }
}
