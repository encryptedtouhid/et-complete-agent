using System.ComponentModel.DataAnnotations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Application.Tests.Configuration;

public sealed class PersistenceOptionsTests
{
    [Theory]
    [InlineData(ConversationStoreKind.Sqlite)]
    [InlineData(ConversationStoreKind.SqlServer)]
    [InlineData(ConversationStoreKind.AzureSql)]
    [InlineData(ConversationStoreKind.Postgres)]
    [InlineData(ConversationStoreKind.MySql)]
    public void Relational_RequiresConnectionString(ConversationStoreKind kind)
    {
        var opts = new PersistenceOptions
        {
            ConversationStore = kind,
            ConnectionString = string.Empty
        };

        var results = Validate(opts);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PersistenceOptions.ConnectionString)));
    }

    [Theory]
    [InlineData(ConversationStoreKind.Sqlite, "Data Source=:memory:")]
    [InlineData(ConversationStoreKind.SqlServer, "Server=localhost;Database=x;Trusted_Connection=True;")]
    [InlineData(ConversationStoreKind.AzureSql, "Server=tcp:x.database.windows.net,1433;Database=x;")]
    [InlineData(ConversationStoreKind.Postgres, "Host=localhost;Username=u;Password=p;Database=x")]
    [InlineData(ConversationStoreKind.MySql, "Server=localhost;Database=x;Uid=u;Pwd=p")]
    public void Relational_AcceptsValidConnectionString(ConversationStoreKind kind, string connStr)
    {
        var opts = new PersistenceOptions
        {
            ConversationStore = kind,
            ConnectionString = connStr
        };

        Assert.Empty(Validate(opts));
    }

    [Fact]
    public void InMemory_RequiresNothing()
    {
        var opts = new PersistenceOptions { ConversationStore = ConversationStoreKind.InMemory };

        Assert.Empty(Validate(opts));
    }

    [Fact]
    public void Cosmos_RequiresConnectionStringOrEndpointKey()
    {
        var opts = new PersistenceOptions
        {
            ConversationStore = ConversationStoreKind.Cosmos,
            Cosmos = new CosmosOptions
            {
                ConnectionString = null,
                AccountEndpoint = null,
                AccountKey = null
            }
        };

        var results = Validate(opts);

        Assert.Contains(results, r => r.ErrorMessage!.Contains("Cosmos", StringComparison.Ordinal));
    }

    [Fact]
    public void Cosmos_AcceptsConnectionString()
    {
        var opts = new PersistenceOptions
        {
            ConversationStore = ConversationStoreKind.Cosmos,
            Cosmos = new CosmosOptions
            {
                ConnectionString = "AccountEndpoint=https://x.documents.azure.com:443/;AccountKey=AAA==;"
            }
        };

        Assert.Empty(Validate(opts));
    }

    [Fact]
    public void Cosmos_AcceptsEndpointAndKey()
    {
        var opts = new PersistenceOptions
        {
            ConversationStore = ConversationStoreKind.Cosmos,
            Cosmos = new CosmosOptions
            {
                AccountEndpoint = "https://x.documents.azure.com:443/",
                AccountKey = "AAA=="
            }
        };

        Assert.Empty(Validate(opts));
    }

    [Fact]
    public void Mongo_RequiresConnectionString()
    {
        var opts = new PersistenceOptions
        {
            ConversationStore = ConversationStoreKind.Mongo,
            ConnectionString = string.Empty
        };

        var results = Validate(opts);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PersistenceOptions.ConnectionString)));
    }

    [Fact]
    public void Mongo_AcceptsMongoUri()
    {
        var opts = new PersistenceOptions
        {
            ConversationStore = ConversationStoreKind.Mongo,
            ConnectionString = "mongodb://localhost:27017"
        };

        Assert.Empty(Validate(opts));
    }

    private static List<ValidationResult> Validate(PersistenceOptions opts)
        => opts.Validate(new ValidationContext(opts)).ToList();
}
