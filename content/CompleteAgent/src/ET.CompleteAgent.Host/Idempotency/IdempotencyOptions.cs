using System.ComponentModel.DataAnnotations;

namespace ET.CompleteAgent.Host.Idempotency;

internal sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";
    public const string HeaderName = "Idempotency-Key";

    public bool Enabled { get; init; } = true;

    [Range(1, 1440)]
    public int TtlMinutes { get; init; } = 10;

    [Range(1, 256)]
    public int MaxKeyLength { get; init; } = 128;
}
