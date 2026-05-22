namespace ET.CompleteAgent.Host.Budgeting;

internal interface ITokenUsageTracker
{
    long GetUsage(string subjectKey, DateOnly day);

    void Increment(string subjectKey, DateOnly day, long tokens);
}
