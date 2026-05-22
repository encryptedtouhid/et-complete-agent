using System.ComponentModel.DataAnnotations;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Entities;

public sealed class ConversationMessageEntity
{
    [Key]
    public long Id { get; init; }

    [Required, MaxLength(128)]
    public string ConversationId { get; init; } = string.Empty;

    [Required, MaxLength(32)]
    public string Role { get; init; } = string.Empty;

    [Required]
    public string Content { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}
