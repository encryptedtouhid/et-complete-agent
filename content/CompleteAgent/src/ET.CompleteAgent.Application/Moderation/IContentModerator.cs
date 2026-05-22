namespace ET.CompleteAgent.Application.Moderation;

public sealed record ModerationResult(bool IsAllowed, string? Reason = null, double Severity = 0);

public interface IContentModerator
{
    Task<ModerationResult> ModerateAsync(string content, CancellationToken cancellationToken = default);
}

public sealed class NoOpContentModerator : IContentModerator
{
    public Task<ModerationResult> ModerateAsync(string content, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ModerationResult(IsAllowed: true));
}
