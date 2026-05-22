using Azure.AI.OpenAI;
using Azure.Identity;
using ET.CompleteAgent.Application.Agents;
using ET.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace ET.CompleteAgent.Infrastructure.Llm;

public sealed class ChatAgentFactory : IChatAgentFactory
{
    private readonly ChatClient _chatClient;

    public ChatAgentFactory(IOptions<AgentOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _chatClient = BuildClient(options.Value);
    }

    public AIAgent Create(string name, string instructions, IEnumerable<AIFunction>? tools = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions);

        var toolArray = tools?.Cast<AITool>().ToArray();
        return _chatClient.AsAIAgent(instructions: instructions, name: name, tools: toolArray);
    }

    private static ChatClient BuildClient(AgentOptions options) => options.Provider switch
    {
        AgentProvider.AzureOpenAI => BuildAzureOpenAI(options),
        AgentProvider.OpenAI      => BuildOpenAI(options),
        _ => throw new InvalidOperationException($"Unsupported provider: {options.Provider}")
    };

    private static ChatClient BuildAzureOpenAI(AgentOptions options)
    {
        var endpoint = options.AzureOpenAI.Endpoint
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint must be set.");

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        return azureClient.GetChatClient(options.Model);
    }

    private static ChatClient BuildOpenAI(AgentOptions options)
    {
        var apiKey = options.OpenAI.ApiKey
            ?? throw new InvalidOperationException("OpenAI:ApiKey must be set via user-secrets or environment.");

        var openAiClient = new OpenAIClient(apiKey);
        return openAiClient.GetChatClient(options.Model);
    }
}
