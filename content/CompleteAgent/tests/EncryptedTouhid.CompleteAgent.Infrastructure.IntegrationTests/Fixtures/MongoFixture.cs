using Testcontainers.MongoDb;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;

public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
