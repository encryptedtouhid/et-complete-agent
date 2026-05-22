using System.ComponentModel.DataAnnotations;

namespace ET.CompleteAgent.Infrastructure.Configuration;

public sealed class AgentOptions : IValidatableObject
{
    public const string SectionName = "Agent";

    [Required]
    public AgentProvider Provider { get; init; } = AgentProvider.AzureOpenAI;

    [Required, MinLength(1)]
    public string Model { get; init; } = "gpt-4o-mini";

    public AzureOpenAISettings AzureOpenAI { get; init; } = new();

    public OpenAISettings OpenAI { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        switch (Provider)
        {
            case AgentProvider.AzureOpenAI:
                if (string.IsNullOrWhiteSpace(AzureOpenAI.Endpoint))
                {
                    yield return new ValidationResult(
                        $"{SectionName}:AzureOpenAI:Endpoint is required when Provider = AzureOpenAI.",
                        [nameof(AzureOpenAI)]);
                }
                break;

            case AgentProvider.OpenAI:
                if (string.IsNullOrWhiteSpace(OpenAI.ApiKey))
                {
                    yield return new ValidationResult(
                        $"{SectionName}:OpenAI:ApiKey is required when Provider = OpenAI. Set it via user-secrets or the OPENAI_API_KEY env var — never appsettings.json.",
                        [nameof(OpenAI)]);
                }
                break;

            default:
                yield return new ValidationResult(
                    $"Unknown Provider value: {Provider}",
                    [nameof(Provider)]);
                break;
        }
    }
}

public enum AgentProvider
{
    AzureOpenAI = 0,
    OpenAI = 1
}

public sealed class AzureOpenAISettings
{
    public string? Endpoint { get; init; }
}

public sealed class OpenAISettings
{
    public string? ApiKey { get; init; }
}
