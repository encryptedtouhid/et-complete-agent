using System.ComponentModel.DataAnnotations;

namespace EncryptedTouhid.CompleteAgent.Host.RateLimiting;

internal sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    [Required]
    public RateLimitStoreKind Store { get; init; } = RateLimitStoreKind.InMemory;

    [Range(1, 10_000)]
    public int PermitLimit { get; init; } = 60;

    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 60;

    public string RedisConnectionString { get; init; } = "localhost:6379";
}

internal enum RateLimitStoreKind
{
    InMemory = 0,
    Redis = 1
}
