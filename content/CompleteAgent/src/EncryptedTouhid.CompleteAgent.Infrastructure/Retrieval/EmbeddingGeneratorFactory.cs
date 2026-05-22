using Azure.AI.OpenAI;
using Azure.Identity;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Retrieval;

public static class EmbeddingGeneratorFactory
{
    public static IEmbeddingGenerator<string, Embedding<float>> Create(
        IOptions<AgentOptions> options,
        IOptions<RetrievalOptions> retrieval)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(retrieval);

        var agent = options.Value;
        var model = retrieval.Value.EmbeddingModel;

        return agent.Provider switch
        {
            AgentProvider.AzureOpenAI => BuildAzure(agent, model),
            AgentProvider.OpenAI => BuildOpenAI(agent, model),
            _ => throw new InvalidOperationException($"Unsupported provider: {agent.Provider}")
        };
    }

    private static IEmbeddingGenerator<string, Embedding<float>> BuildAzure(AgentOptions agent, string model)
    {
        var endpoint = agent.AzureOpenAI.Endpoint
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint must be set.");
        var client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        return client.GetEmbeddingClient(model).AsIEmbeddingGenerator();
    }

    private static IEmbeddingGenerator<string, Embedding<float>> BuildOpenAI(AgentOptions agent, string model)
    {
        var apiKey = agent.OpenAI.ApiKey
            ?? throw new InvalidOperationException("OpenAI:ApiKey must be set.");
        var client = new OpenAIClient(apiKey);
        return client.GetEmbeddingClient(model).AsIEmbeddingGenerator();
    }
}
