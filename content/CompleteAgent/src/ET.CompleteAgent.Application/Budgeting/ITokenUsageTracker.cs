namespace ET.CompleteAgent.Application.Budgeting;

public interface ITokenUsageTracker
{
    long GetUsage(string subjectKey, DateOnly day);

    void Increment(string subjectKey, DateOnly day, long tokens);
}
