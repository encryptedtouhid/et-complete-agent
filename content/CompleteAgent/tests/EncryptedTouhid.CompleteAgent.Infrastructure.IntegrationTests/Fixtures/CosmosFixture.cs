using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;

public sealed class CosmosFixture : IAsyncLifetime
{
    private readonly CosmosDbContainer _container = new CosmosDbBuilder().Build();

    public string ConnectionString => _container.GetConnectionString();

    public CosmosClient CreateClient() => new(
        ConnectionString,
        new CosmosClientOptions
        {
            // Emulator uses a self-signed cert; trust it for tests only.
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            }),
            ConnectionMode = ConnectionMode.Gateway
        });

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
