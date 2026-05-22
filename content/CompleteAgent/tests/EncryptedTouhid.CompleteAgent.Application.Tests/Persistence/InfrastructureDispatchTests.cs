using EncryptedTouhid.CompleteAgent.Application.Audit;
using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using EncryptedTouhid.CompleteAgent.Infrastructure.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Audit;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Cosmos;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Application.Tests.Persistence;

public sealed class InfrastructureDispatchTests
{
    [Fact]
    public void InMemory_RegistersInMemoryStoreAndNoOpAudit()
    {
        using var sp = Build(new Dictionary<string, string?>
        {
            ["Persistence:ConversationStore"] = "InMemory",
            ["Agent:Provider"] = "OpenAI",
            ["Agent:Model"] = "gpt-test",
            ["Agent:OpenAI:ApiKey"] = "test"
        });

        Assert.IsType<InMemoryConversationStore>(sp.GetRequiredService<IConversationStore>());
        Assert.IsType<NoOpAuditLog>(sp.GetRequiredService<IAuditLog>());
    }

    [Theory]
    [InlineData("Sqlite", "Data Source=:memory:")]
    [InlineData("SqlServer", "Server=localhost;Database=x;Trusted_Connection=True;")]
    [InlineData("AzureSql", "Server=tcp:x.database.windows.net,1433;Database=x;User Id=u;Password=p;")]
    [InlineData("Postgres", "Host=localhost;Username=u;Password=p;Database=x")]
    public void Relational_RegistersEfCoreStoreAndAuditLog(string kind, string connStr)
    {
        using var sp = Build(new Dictionary<string, string?>
        {
            ["Persistence:ConversationStore"] = kind,
            ["Persistence:ConnectionString"] = connStr,
            ["Agent:Provider"] = "OpenAI",
            ["Agent:Model"] = "gpt-test",
            ["Agent:OpenAI:ApiKey"] = "test"
        });

        Assert.IsType<EfCoreConversationStore>(sp.GetRequiredService<IConversationStore>());
        Assert.IsType<EfCoreAuditLog>(sp.GetRequiredService<IAuditLog>());
    }

    [Fact]
    public void MySql_RegistersEfCoreStore()
    {
        using var sp = Build(new Dictionary<string, string?>
        {
            ["Persistence:ConversationStore"] = "MySql",
            ["Persistence:ConnectionString"] = "Server=localhost;Database=x;Uid=u;Pwd=p",
            ["Agent:Provider"] = "OpenAI",
            ["Agent:Model"] = "gpt-test",
            ["Agent:OpenAI:ApiKey"] = "test"
        });

        Assert.IsType<EfCoreConversationStore>(sp.GetRequiredService<IConversationStore>());
    }

    [Fact]
    public void Cosmos_RegistersCosmosStoreAndAudit()
    {
        using var sp = Build(new Dictionary<string, string?>
        {
            ["Persistence:ConversationStore"] = "Cosmos",
            ["Persistence:Cosmos:AccountEndpoint"] = "https://localhost:8081/",
            ["Persistence:Cosmos:AccountKey"] = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Persistence:Cosmos:Database"] = "test",
            ["Agent:Provider"] = "OpenAI",
            ["Agent:Model"] = "gpt-test",
            ["Agent:OpenAI:ApiKey"] = "test"
        });

        Assert.IsType<CosmosConversationStore>(sp.GetRequiredService<IConversationStore>());
        Assert.IsType<CosmosAuditLog>(sp.GetRequiredService<IAuditLog>());
    }

    [Fact]
    public void Mongo_RegistersMongoStoreAndAudit()
    {
        using var sp = Build(new Dictionary<string, string?>
        {
            ["Persistence:ConversationStore"] = "Mongo",
            ["Persistence:ConnectionString"] = "mongodb://localhost:27017",
            ["Agent:Provider"] = "OpenAI",
            ["Agent:Model"] = "gpt-test",
            ["Agent:OpenAI:ApiKey"] = "test"
        });

        Assert.IsType<MongoConversationStore>(sp.GetRequiredService<IConversationStore>());
        Assert.IsType<MongoAuditLog>(sp.GetRequiredService<IAuditLog>());
    }

    private static ServiceProvider Build(IDictionary<string, string?> overrides)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(overrides).Build();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddLogging();
        services.AddInfrastructure(config);
        return services.BuildServiceProvider();
    }
}
