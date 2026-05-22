using ET.CompleteAgent.Application.Agents;
using ET.CompleteAgent.Application.Budgeting;
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
        var tracker = new InMemoryTokenUsageTracker();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var timeTool = new GetCurrentTimeTool(timeProvider);
        var searchTool = new SearchKnowledgeBaseTool(NullLogger<SearchKnowledgeBaseTool>.Instance);
        var retryPolicy = new RetryPolicy(1, TimeSpan.FromMilliseconds(1), NullLogger<RetryPolicy>.Instance);
        var logger = NullLogger<AgentRunner>.Instance;

        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(null!, promptLoader, conversationStore, moderator, tracker, timeProvider, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, null!, conversationStore, moderator, tracker, timeProvider, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, null!, moderator, tracker, timeProvider, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, null!, tracker, timeProvider, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, null!, timeProvider, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, tracker, null!, timeTool, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, tracker, timeProvider, null!, searchTool, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, tracker, timeProvider, timeTool, null!, retryPolicy, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, tracker, timeProvider, timeTool, searchTool, null!, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new AgentRunner(agentFactory, promptLoader, conversationStore, moderator, tracker, timeProvider, timeTool, searchTool, retryPolicy, null!));
    }
}
