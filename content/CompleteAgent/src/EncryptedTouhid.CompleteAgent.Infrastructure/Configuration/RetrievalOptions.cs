using System.ComponentModel.DataAnnotations;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    [Required, MinLength(1)]
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";

    [Required]
    public VectorStoreKind VectorStore { get; init; } = VectorStoreKind.InMemory;

    public QdrantSettings Qdrant { get; init; } = new();
}

public enum VectorStoreKind
{
    InMemory = 0,
    Qdrant = 1
}

public sealed class QdrantSettings
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 6334;
    public bool UseTls { get; init; }
    public string? ApiKey { get; init; }
    public string Collection { get; init; } = "agent-docs";

    [Range(32, 8192)]
    public int VectorSize { get; init; } = 1536;
}
