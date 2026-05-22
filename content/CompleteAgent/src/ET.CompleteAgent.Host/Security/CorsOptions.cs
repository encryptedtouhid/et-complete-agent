namespace ET.CompleteAgent.Host.Security;

internal sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "AgentCors";

    public bool Enabled { get; init; }

    public IList<string> AllowedOrigins { get; init; } = [];
}
