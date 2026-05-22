using System.ComponentModel.DataAnnotations;

namespace ET.CompleteAgent.Application.Resilience;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    [Range(1, 10)]
    public int MaxRetryAttempts { get; init; } = 3;

    [Range(1, 600)]
    public int BackoffSeconds { get; init; } = 2;
}
