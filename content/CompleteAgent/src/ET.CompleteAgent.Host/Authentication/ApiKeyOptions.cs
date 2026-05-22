using System.ComponentModel.DataAnnotations;

namespace ET.CompleteAgent.Host.Authentication;

internal sealed class ApiKeyOptions
{
    public const string SectionName = "Authentication";
    public const string HeaderName = "X-API-Key";

    [Required, MinLength(16)]
    public string ApiKey { get; init; } = string.Empty;
}
