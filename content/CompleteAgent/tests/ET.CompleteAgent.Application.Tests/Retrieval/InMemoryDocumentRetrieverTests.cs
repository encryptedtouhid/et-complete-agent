using ET.CompleteAgent.Application.Retrieval;
using ET.CompleteAgent.Infrastructure.Retrieval;
using Microsoft.Extensions.AI;
using Xunit;

namespace ET.CompleteAgent.Application.Tests.Retrieval;

public sealed class InMemoryDocumentRetrieverTests
{
    [Fact]
    public async Task SearchAsync_RanksRelevantDocumentFirst()
    {
        using var embedder = new DeterministicEmbedder();
        var retriever = new InMemoryDocumentRetriever(embedder);

        await retriever.IndexAsync("doc-cats", "Cats are independent feline mammals.");
        await retriever.IndexAsync("doc-dogs", "Dogs are loyal canine companions.");
        await retriever.IndexAsync("doc-cars", "Cars are mechanical vehicles for transportation.");

        var results = await retriever.SearchAsync("feline animals", topK: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("doc-cats", results[0].Id);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_OnEmptyIndex()
    {
        using var embedder = new DeterministicEmbedder();
        var retriever = new InMemoryDocumentRetriever(embedder);

        var results = await retriever.SearchAsync("anything");

        Assert.Empty(results);
    }

    [Fact]
    public async Task IndexAsync_OverwritesExistingDocument()
    {
        using var embedder = new DeterministicEmbedder();
        var retriever = new InMemoryDocumentRetriever(embedder);

        await retriever.IndexAsync("doc-1", "original content about cats");
        await retriever.IndexAsync("doc-1", "updated content about dogs");

        var results = await retriever.SearchAsync("dogs", topK: 1);
        Assert.Single(results);
        Assert.Contains("dogs", results[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class DeterministicEmbedder : IEmbeddingGenerator<string, Embedding<float>>
    {
        public EmbeddingGeneratorMetadata Metadata { get; } = new("deterministic");

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var list = values.Select(v => new Embedding<float>(Vectorize(v))).ToList();
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(list));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType == typeof(EmbeddingGeneratorMetadata) ? Metadata : null;

        public void Dispose() { }

        private static float[] Vectorize(string text) =>
        [
            Score(text, ["cat", "feline", "kitten"]),
            Score(text, ["dog", "canine", "puppy"]),
            Score(text, ["car", "vehicle", "auto"]),
            Score(text, ["animal", "mammal", "pet"])
        ];

        private static float Score(string text, IEnumerable<string> keywords) =>
            keywords.Count(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
