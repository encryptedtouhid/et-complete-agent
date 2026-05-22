using ET.CompleteAgent.Application.Tools;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace ET.CompleteAgent.Application.Tests.Tools;

public sealed class GetCurrentTimeToolTests
{
    [Fact]
    public void GetCurrentTimeUtc_ReturnsIso8601Utc()
    {
        var fixedTime = new DateTimeOffset(2026, 5, 22, 14, 3, 12, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var tool = new GetCurrentTimeTool(timeProvider);

        var result = tool.GetCurrentTimeUtc();

        Assert.Equal("2026-05-22T14:03:12.0000000Z", result);
    }
}
