using System.Collections.Concurrent;
using EncryptedTouhid.CompleteAgent.Application.Retrieval;
using Microsoft.Extensions.AI;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Retrieval;

public sealed class InMemoryDocumentRetriever : IDocumentRetriever
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private readonly ConcurrentDictionary<string, IndexedDocument> _store = new(StringComparer.Ordinal);

    public InMemoryDocumentRetriever(IEmbeddingGenerator<string, Embedding<float>> embedder)
    {
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
    }

    public async Task IndexAsync(string id, string content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var embedding = await _embedder.GenerateAsync(content, cancellationToken: cancellationToken);
        _store[id] = new IndexedDocument(content, embedding.Vector.ToArray());
    }

    public async Task<IReadOnlyList<RetrievedDocument>> SearchAsync(
        string query,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(topK, 1);

        if (_store.IsEmpty)
        {
            return Array.Empty<RetrievedDocument>();
        }

        var queryEmbedding = (await _embedder.GenerateAsync(query, cancellationToken: cancellationToken)).Vector.ToArray();

        return _store
            .Select(kvp => new RetrievedDocument(
                kvp.Key,
                kvp.Value.Content,
                CosineSimilarity(queryEmbedding, kvp.Value.Embedding)))
            .OrderByDescending(d => d.Score)
            .Take(topK)
            .ToArray();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            return 0.0;
        }

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom < double.Epsilon ? 0.0 : dot / denom;
    }

    private sealed record IndexedDocument(string Content, float[] Embedding);
}
