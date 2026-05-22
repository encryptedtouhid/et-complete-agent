using EncryptedTouhid.CompleteAgent.Application.Guardrails;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Application.Tests.Guardrails;

public sealed class OutputGuardrailTests
{
    [Theory]
    [InlineData("Reach me at alice@example.com please", "alice@example.com")]
    [InlineData("token sk-abcdefghijklmnopqrstuvwxyz1234567890", "sk-abcdefghijklmnopqrstuvwxyz1234567890")]
    [InlineData("Call +1 (555) 010-1234 tomorrow", "+1 (555) 010-1234")]
    public void Scrub_RedactsKnownPiiPatterns(string input, string sensitiveSubstring)
    {
        var result = OutputGuardrail.Scrub(input);

        Assert.DoesNotContain(sensitiveSubstring, result, StringComparison.Ordinal);
        Assert.Contains("[redacted]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Scrub_PassesThroughBenignText()
    {
        var result = OutputGuardrail.Scrub("The capital of France is Paris.");

        Assert.Equal("The capital of France is Paris.", result);
    }

    [Fact]
    public void Scrub_HandlesNullAndEmpty()
    {
        Assert.Equal(string.Empty, OutputGuardrail.Scrub(null));
        Assert.Equal(string.Empty, OutputGuardrail.Scrub(string.Empty));
    }
}
