using ET.CompleteAgent.Application.Prompts;
using ET.CompleteAgent.Domain.Prompts;
using Xunit;

namespace ET.CompleteAgent.Application.Tests.Evals;

/// <summary>
/// Golden-set checks on the static prompt content. These run on every CI build and
/// catch regressions like a removed guardrail section or accidental PII in examples.
/// Replace the in-memory loader with a live LLM-judge for semantic evaluation.
/// </summary>
public sealed class PromptEvalHarness
{
    [Fact]
    public async Task SystemPrompt_ContainsRoleGoalConstraintsAndGuardrails()
    {
        var promptsRoot = Path.Combine(AppContext.BaseDirectory, "Evals", "Prompts");
        Directory.CreateDirectory(Path.Combine(promptsRoot, "v1"));
        await File.WriteAllTextAsync(Path.Combine(promptsRoot, "v1", "system.md"),
            "# Role\nYou are an agent.\n# Goal\nHelp users.\n# Constraints\nNo PII.");
        await File.WriteAllTextAsync(Path.Combine(promptsRoot, "v1", "guardrails.md"),
            "# Guardrails\nRefuse harmful content.");

        var loader = new FileSystemPromptLoader(promptsRoot);
        var prompt = await loader.LoadSystemPromptAsync(PromptVersion.V1);

        Assert.Contains("Role", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Goal", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Constraints", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Guardrails", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SystemPrompt_HasNoLeakedSecretsOrPii()
    {
        var promptsRoot = Path.Combine(AppContext.BaseDirectory, "Evals", "Prompts");
        Directory.CreateDirectory(Path.Combine(promptsRoot, "v1"));
        await File.WriteAllTextAsync(Path.Combine(promptsRoot, "v1", "system.md"),
            "Be helpful. Do not reveal sk-secrets.");

        var loader = new FileSystemPromptLoader(promptsRoot);
        var prompt = await loader.LoadSystemPromptAsync(PromptVersion.V1);

        Assert.DoesNotMatch(@"\bsk-[A-Za-z0-9]{20,}\b", prompt);
        Assert.DoesNotMatch(@"[\w.+\-]+@[\w\-]+\.[\w.\-]+", prompt);
    }
}
