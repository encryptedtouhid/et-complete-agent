using System.Diagnostics;

namespace EncryptedTouhid.CompleteAgent.Application.Telemetry;

public static class AgentDiagnostics
{
    public const string ActivitySourceName = "EncryptedTouhid.CompleteAgent";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
