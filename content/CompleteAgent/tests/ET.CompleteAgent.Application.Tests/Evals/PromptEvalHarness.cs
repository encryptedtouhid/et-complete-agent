using ET.CompleteAgent.Application.Prompts;
using ET.CompleteAgent.Domain.Prompts;
using Xunit;

namespace ET.CompleteAgent.Application.Tests.Evals;

/// <summary>
/// Golden-set checks against the actual prompt files shipped in the Host project.
/// These run on every CI build and catch regressions like a removed guardrail section
/// or accidental PII in examples. Swap the in-process loader for a live LLM-judge for
/// semantic evaluation.
/// </summary>
public sealed class PromptEvalHarness
{
    private static FileSystemPromptLoader Loader =>
        new(Path.Combine(AppContext.BaseDirectory, "Prompts"));

    [Fact]
    public async Task SystemPrompt_ContainsRoleGoalConstraintsAndGuardrails()
    {
        var prompt = await Loader.LoadSystemPromptAsync(PromptVersion.V1);

        Assert.Contains("Role", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Goal", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Constraints", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Guardrails", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SystemPrompt_ContainsPromptInjectionMitigation()
    {
        var prompt = await Loader.LoadSystemPromptAsync(PromptVersion.V1);

        Assert.Contains("user_input", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("instructions", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SystemPrompt_HasNoLeakedSecretsOrPii()
    {
        var prompt = await Loader.LoadSystemPromptAsync(PromptVersion.V1);

        Assert.DoesNotMatch(@"\bsk-[A-Za-z0-9]{20,}\b", prompt);
        Assert.DoesNotMatch(@"\bAKIA[0-9A-Z]{16}\b", prompt);
        Assert.DoesNotMatch(@"\bghp_[A-Za-z0-9]{36,}\b", prompt);
    }

    [Fact]
    public async Task SystemPrompt_DefinesOutputFormat()
    {
        var prompt = await Loader.LoadSystemPromptAsync(PromptVersion.V1);

        Assert.Contains("Output Format", prompt, StringComparison.OrdinalIgnoreCase);
    }
}
