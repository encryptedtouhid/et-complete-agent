using Testcontainers.MySql;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;

public sealed class MySqlFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder().Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
