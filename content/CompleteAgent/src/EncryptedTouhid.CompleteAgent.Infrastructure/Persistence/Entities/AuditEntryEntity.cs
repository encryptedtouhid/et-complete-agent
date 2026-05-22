using System.ComponentModel.DataAnnotations;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Entities;

public sealed class AuditEntryEntity
{
    [Key]
    public long Id { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    [Required, MaxLength(128)]
    public string SubjectId { get; init; } = string.Empty;

    [MaxLength(256)]
    public string? ConversationId { get; init; }

    [Required, MaxLength(64)]
    public string Operation { get; init; } = string.Empty;

    [MaxLength(512)]
    public string InputPreview { get; init; } = string.Empty;

    public bool Success { get; init; }

    public long TokenCount { get; init; }

    public long DurationMs { get; init; }
}
