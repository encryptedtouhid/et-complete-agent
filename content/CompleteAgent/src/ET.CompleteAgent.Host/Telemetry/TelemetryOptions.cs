namespace ET.CompleteAgent.Host.Telemetry;

internal sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string ServiceName { get; init; } = "complete-agent";

    public string? OtlpEndpoint { get; init; }

    public bool EnableConsoleExporter { get; init; } = true;
}
