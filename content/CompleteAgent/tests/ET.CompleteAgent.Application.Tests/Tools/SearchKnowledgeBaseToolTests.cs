using ET.CompleteAgent.Application.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ET.CompleteAgent.Application.Tests.Tools;

public sealed class SearchKnowledgeBaseToolTests
{
    private readonly SearchKnowledgeBaseTool _tool = new(NullLogger<SearchKnowledgeBaseTool>.Instance);

    [Theory]
    [InlineData("agent framework")]
    [InlineData("Agent Framework")]
    [InlineData("clean architecture")]
    [InlineData("prompt injection")]
    public void Search_ReturnsSnippet_WhenQueryMatchesIndex(string query)
    {
        var result = _tool.Search(query);

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("totally unknown topic")]
    public void Search_ReturnsEmpty_ForBlankOrUnknownQuery(string query)
    {
        var result = _tool.Search(query);

        Assert.Equal(string.Empty, result);
    }
}
