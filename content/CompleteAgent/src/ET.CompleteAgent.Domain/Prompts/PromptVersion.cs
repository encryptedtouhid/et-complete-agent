namespace ET.CompleteAgent.Domain.Prompts;

public sealed record PromptVersion(string Folder)
{
    public static readonly PromptVersion V1 = new("v1");
}
