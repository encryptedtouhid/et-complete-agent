using System.Diagnostics;

namespace ET.CompleteAgent.Application.Telemetry;

public static class AgentDiagnostics
{
    public const string ActivitySourceName = "ET.CompleteAgent";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
