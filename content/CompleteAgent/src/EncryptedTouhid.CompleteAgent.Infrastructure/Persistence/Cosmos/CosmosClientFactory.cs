using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Cosmos;

internal static class CosmosClientFactory
{
    public static CosmosClient Create(CosmosOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var clientOptions = new CosmosClientOptions
        {
            ApplicationName = "complete-agent",
            ConnectionMode = ConnectionMode.Direct,
            ConsistencyLevel = ConsistencyLevel.Session
        };

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new CosmosClient(options.ConnectionString, clientOptions);
        }

        if (string.IsNullOrWhiteSpace(options.AccountEndpoint) || string.IsNullOrWhiteSpace(options.AccountKey))
        {
            throw new InvalidOperationException(
                "CosmosOptions requires either ConnectionString or AccountEndpoint + AccountKey.");
        }

        return new CosmosClient(options.AccountEndpoint, options.AccountKey, clientOptions);
    }
}
