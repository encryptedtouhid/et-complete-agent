namespace ET.CompleteAgent.Application.Audit;

public sealed record AuditEntry(
    DateTimeOffset Timestamp,
    string SubjectId,
    string? ConversationId,
    string Operation,
    string InputPreview,
    bool Success,
    long TokenCount,
    long DurationMs);

public interface IAuditLog
{
    Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}

public sealed class NoOpAuditLog : IAuditLog
{
    public Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
