using System.ComponentModel.DataAnnotations;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;

public sealed class PersistenceOptions : IValidatableObject
{
    public const string SectionName = "Persistence";

    [Required]
    public ConversationStoreKind ConversationStore { get; init; } = ConversationStoreKind.InMemory;

    /// <summary>
    /// Connection string for relational backends (Sqlite, SqlServer, AzureSql, Postgres, MySql).
    /// MongoDB also reads from here (mongodb:// URI).
    /// </summary>
    public string ConnectionString { get; init; } = "Data Source=completeagent.db";

    public CosmosOptions Cosmos { get; init; } = new();

    public MongoOptions Mongo { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        switch (ConversationStore)
        {
            case ConversationStoreKind.InMemory:
                yield break;

            case ConversationStoreKind.Sqlite:
            case ConversationStoreKind.SqlServer:
            case ConversationStoreKind.AzureSql:
            case ConversationStoreKind.Postgres:
            case ConversationStoreKind.MySql:
                if (string.IsNullOrWhiteSpace(ConnectionString))
                {
                    yield return new ValidationResult(
                        $"Persistence:ConnectionString is required when ConversationStore = {ConversationStore}.",
                        [nameof(ConnectionString)]);
                }
                yield break;

            case ConversationStoreKind.Cosmos:
                foreach (var result in Cosmos.Validate())
                {
                    yield return result;
                }
                yield break;

            case ConversationStoreKind.Mongo:
                if (string.IsNullOrWhiteSpace(ConnectionString))
                {
                    yield return new ValidationResult(
                        "Persistence:ConnectionString is required when ConversationStore = Mongo (a mongodb:// URI).",
                        [nameof(ConnectionString)]);
                }
                foreach (var result in Mongo.Validate())
                {
                    yield return result;
                }
                yield break;

            default:
                yield return new ValidationResult(
                    $"Unknown Persistence:ConversationStore value '{ConversationStore}'.",
                    [nameof(ConversationStore)]);
                yield break;
        }
    }
}

public enum ConversationStoreKind
{
    InMemory = 0,
    Sqlite = 1,
    SqlServer = 2,
    AzureSql = 3,
    Postgres = 4,
    MySql = 5,
    Cosmos = 6,
    Mongo = 7
}

public sealed class CosmosOptions
{
    /// <summary>
    /// Full Cosmos DB account connection string (preferred for managed identity-less setups).
    /// If set, takes precedence over AccountEndpoint + AccountKey.
    /// </summary>
    public string? ConnectionString { get; init; }

    public string? AccountEndpoint { get; init; }

    public string? AccountKey { get; init; }

    [Required]
    public string Database { get; init; } = "completeagent";

    [Required]
    public string ConversationsContainer { get; init; } = "conversations";

    [Required]
    public string AuditContainer { get; init; } = "audit";

    /// <summary>
    /// Throughput (RU/s) to provision when auto-creating the database. Null means use
    /// container-level throughput (autoscale). Production setups should pre-create containers.
    /// </summary>
    public int? DatabaseThroughput { get; init; }

    public IEnumerable<ValidationResult> Validate()
    {
        var hasConnectionString = !string.IsNullOrWhiteSpace(ConnectionString);
        var hasEndpointAndKey = !string.IsNullOrWhiteSpace(AccountEndpoint)
                                && !string.IsNullOrWhiteSpace(AccountKey);

        if (!hasConnectionString && !hasEndpointAndKey)
        {
            yield return new ValidationResult(
                "Persistence:Cosmos requires either ConnectionString, or AccountEndpoint + AccountKey.",
                [nameof(ConnectionString), nameof(AccountEndpoint), nameof(AccountKey)]);
        }

        if (string.IsNullOrWhiteSpace(Database))
        {
            yield return new ValidationResult("Persistence:Cosmos:Database is required.", [nameof(Database)]);
        }

        if (string.IsNullOrWhiteSpace(ConversationsContainer))
        {
            yield return new ValidationResult(
                "Persistence:Cosmos:ConversationsContainer is required.",
                [nameof(ConversationsContainer)]);
        }
    }
}

public sealed class MongoOptions
{
    [Required]
    public string Database { get; init; } = "completeagent";

    [Required]
    public string ConversationsCollection { get; init; } = "conversations";

    [Required]
    public string AuditCollection { get; init; } = "audit";

    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(Database))
        {
            yield return new ValidationResult("Persistence:Mongo:Database is required.", [nameof(Database)]);
        }

        if (string.IsNullOrWhiteSpace(ConversationsCollection))
        {
            yield return new ValidationResult(
                "Persistence:Mongo:ConversationsCollection is required.",
                [nameof(ConversationsCollection)]);
        }
    }
}
