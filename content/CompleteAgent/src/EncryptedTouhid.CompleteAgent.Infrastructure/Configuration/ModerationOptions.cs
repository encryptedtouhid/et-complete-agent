using System.ComponentModel.DataAnnotations;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;

public sealed class ModerationOptions
{
    public const string SectionName = "Moderation";

    [Required]
    public ModerationProviderKind Provider { get; init; } = ModerationProviderKind.None;

    public string? AzureEndpoint { get; init; }

    [Range(0, 7)]
    public int MaxAllowedSeverity { get; init; } = 2;
}

public enum ModerationProviderKind
{
    None = 0,
    AzureContentSafety = 1
}
