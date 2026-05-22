using System.ComponentModel.DataAnnotations;

namespace ET.CompleteAgent.Infrastructure.Configuration;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    [Required]
    public ConversationStoreKind ConversationStore { get; init; } = ConversationStoreKind.InMemory;

    public string ConnectionString { get; init; } = "Data Source=completeagent.db";
}

public enum ConversationStoreKind
{
    InMemory = 0,
    Sqlite = 1
}
