using Testcontainers.MsSql;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;

public sealed class MsSqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
