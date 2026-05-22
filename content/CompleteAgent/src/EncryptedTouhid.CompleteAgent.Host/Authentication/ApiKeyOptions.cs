using System.ComponentModel.DataAnnotations;

namespace EncryptedTouhid.CompleteAgent.Host.Authentication;

internal sealed class ApiKeyOptions : IValidatableObject
{
    public const string SectionName = "Authentication";
    public const string HeaderName = "X-API-Key";

    public IList<string> ApiKeys { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ApiKeys.Count == 0)
        {
            yield return new ValidationResult(
                $"{SectionName}:ApiKeys must contain at least one entry. Generate one with: openssl rand -hex 16",
                [nameof(ApiKeys)]);
            yield break;
        }
        foreach (var key in ApiKeys)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length < 16)
            {
                yield return new ValidationResult(
                    "Every API key must be at least 16 characters of high-entropy random data.",
                    [nameof(ApiKeys)]);
                yield break;
            }
        }
    }
}
