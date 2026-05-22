using ET.CompleteAgent.Application.Budgeting;
using Xunit;

namespace ET.CompleteAgent.Application.Tests.Budgeting;

public sealed class InMemoryTokenUsageTrackerTests
{
    [Fact]
    public void Increment_AccumulatesPerSubjectPerDay()
    {
        var tracker = new InMemoryTokenUsageTracker();
        var day = new DateOnly(2026, 5, 22);

        tracker.Increment("alice", day, 100);
        tracker.Increment("alice", day, 250);
        tracker.Increment("bob", day, 50);
        tracker.Increment("alice", day.AddDays(1), 999);

        Assert.Equal(350, tracker.GetUsage("alice", day));
        Assert.Equal(50, tracker.GetUsage("bob", day));
        Assert.Equal(999, tracker.GetUsage("alice", day.AddDays(1)));
        Assert.Equal(0, tracker.GetUsage("alice", day.AddDays(2)));
        Assert.Equal(0, tracker.GetUsage("carol", day));
    }

    [Fact]
    public void GetUsage_ReturnsZero_ForUnknownSubject()
    {
        var tracker = new InMemoryTokenUsageTracker();
        Assert.Equal(0, tracker.GetUsage("nobody", new DateOnly(2026, 1, 1)));
    }
}
