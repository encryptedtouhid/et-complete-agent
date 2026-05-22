using ET.CompleteAgent.Application.Retrieval;
using ET.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace ET.CompleteAgent.Infrastructure.Retrieval;

public sealed class QdrantDocumentRetriever : IDocumentRetriever, IAsyncDisposable
{
    private readonly QdrantClient _client;
    private readonly QdrantSettings _settings;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private bool _initialised;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public QdrantDocumentRetriever(
        IOptions<RetrievalOptions> options,
        IEmbeddingGenerator<string, Embedding<float>> embedder)
    {
        ArgumentNullException.ThrowIfNull(options);
        _settings = options.Value.Qdrant;
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
        _client = new QdrantClient(_settings.Host, _settings.Port, _settings.UseTls, apiKey: _settings.ApiKey);
    }

    public async Task IndexAsync(string id, string content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        await EnsureCollectionAsync(cancellationToken);
        var embedding = await _embedder.GenerateAsync(content, cancellationToken: cancellationToken);

        var point = new PointStruct
        {
            Id = new PointId { Uuid = DeterministicGuid(id).ToString() },
            Vectors = embedding.Vector.ToArray()
        };
        point.Payload["doc_id"] = id;
        point.Payload["content"] = content;

        await _client.UpsertAsync(_settings.Collection, [point], cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<RetrievedDocument>> SearchAsync(
        string query,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(topK, 1);

        await EnsureCollectionAsync(cancellationToken);
        var queryEmbedding = (await _embedder.GenerateAsync(query, cancellationToken: cancellationToken)).Vector.ToArray();

        var results = await _client.SearchAsync(
            _settings.Collection,
            queryEmbedding,
            limit: (ulong)topK,
            cancellationToken: cancellationToken);

        return results
            .Select(point => new RetrievedDocument(
                point.Payload.TryGetValue("doc_id", out var idValue) ? idValue.StringValue : string.Empty,
                point.Payload.TryGetValue("content", out var contentValue) ? contentValue.StringValue : string.Empty,
                point.Score))
            .ToArray();
    }

    private async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        if (_initialised)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialised)
            {
                return;
            }

            var exists = await _client.CollectionExistsAsync(_settings.Collection, cancellationToken);
            if (!exists)
            {
                await _client.CreateCollectionAsync(
                    _settings.Collection,
                    new VectorParams { Size = (ulong)_settings.VectorSize, Distance = Distance.Cosine },
                    cancellationToken: cancellationToken);
            }
            _initialised = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static Guid DeterministicGuid(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(bytes[..16]);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        _initLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
