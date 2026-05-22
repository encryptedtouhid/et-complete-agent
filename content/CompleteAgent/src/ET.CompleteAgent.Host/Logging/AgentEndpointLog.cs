using Microsoft.Extensions.Logging;

namespace ET.CompleteAgent.Host.Logging;

internal static partial class AgentEndpointLog
{
    [LoggerMessage(LogLevel.Information, "Agent request received: {Preview}")]
    public static partial void RequestReceived(ILogger logger, string preview);

    [LoggerMessage(LogLevel.Information, "Agent stream request received: {Preview}")]
    public static partial void StreamRequestReceived(ILogger logger, string preview);
}
