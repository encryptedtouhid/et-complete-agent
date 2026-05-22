using ET.CompleteAgent.Application.Agents;
using ET.CompleteAgent.Application.Conversations;
using ET.CompleteAgent.Application.Moderation;
using ET.CompleteAgent.Application.Prompts;
using ET.CompleteAgent.Application.Resilience;
using ET.CompleteAgent.Application.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace ET.CompleteAgent.Application.Tests.Agents;

public sealed class AgentRunnerTests
{
    [Fact]
    public void Construct_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
    {
        var agentFactory = Substitute.For<IChatAgentFactory>();
        var promptLoader = Substitute.For<IPromptLoader>();
        var conversationStore = Substitute.For<IConversationStore>();
        var moderator = new NoOpContentModerator();
        var timeTool = new GetCurrentTimeTool(new FakeTimeProvider(DateTimeOffset.UnixEpoch));
        var searchTool = new SearchKnowledgeBaseTool(NullLogger<SearchKnowledgeBaseTool>.Instance);
        var retryPolicy = new RetryPolicy(1, TimeSpan.FromMilliseconds(1), NullLogger<RetryPolicy>.Instance);
        var logger = NullLogger<AgentRunner>.Instance;

        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(null!, promptLoader, conversationStore, moderator, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, null!, conversationStore, moderator, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, null!, moderator, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, null!, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, null!, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, timeTool, null!, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, timeTool, searchTool, null!, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, timeTool, searchTool, retryPolicy, null!));
    }
}
