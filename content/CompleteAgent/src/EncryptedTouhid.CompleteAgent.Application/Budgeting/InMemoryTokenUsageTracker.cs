using System.Collections.Concurrent;

namespace EncryptedTouhid.CompleteAgent.Application.Budgeting;

public sealed class InMemoryTokenUsageTracker : ITokenUsageTracker
{
    private readonly ConcurrentDictionary<string, long> _usage = new(StringComparer.Ordinal);

    public long GetUsage(string subjectKey, DateOnly day) =>
        _usage.GetValueOrDefault(Key(subjectKey, day));

    public void Increment(string subjectKey, DateOnly day, long tokens) =>
        _usage.AddOrUpdate(Key(subjectKey, day), tokens, (_, existing) => existing + tokens);

    private static string Key(string subjectKey, DateOnly day) =>
        $"{day:yyyy-MM-dd}:{subjectKey}";
}
