using System.ComponentModel.DataAnnotations;

namespace ET.CompleteAgent.Host.Authentication;

internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public bool Enabled { get; init; }

    public string? Authority { get; init; }

    public string? Audience { get; init; }

    public IList<string> ValidIssuers { get; init; } = [];

    [Range(0, 3600)]
    public int ClockSkewSeconds { get; init; } = 30;
}
